using Kippo.Attribute;
using Kippo.Contexs;
using Kippo.Middleware;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Kippo.Routers;

public class CommandRouter
{
    private record HandlerInfo(MethodInfo Method, object Instance);
    private readonly Dictionary<string, HandlerInfo> _commandHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(CallbackQueryAttribute Attr, HandlerInfo Handler)> _callbackHandlers = new();
    private readonly List<(TextAttribute Attr, HandlerInfo Handler)> _textHandlers = new();
    private readonly List<HandlerInfo> _chatMemberHandlers = new();
    private readonly List<HandlerInfo> _contactHandlers = new();
    private readonly List<IBotMiddleware> _middlewares = new();
    private readonly ILogger? _logger;

    public CommandRouter(object handlerInstance, ILogger? logger = null)
    {
        _logger = logger;
        RegisterHandlers(handlerInstance);
    }

    public void Use(IBotMiddleware middleware)
    {
        _middlewares.Add(middleware);
    }

    private void RegisterHandlers(object instance)
    {
        var type = instance.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var handlerInfo = new HandlerInfo(method, instance);

            foreach (var cmdAttr in method.GetCustomAttributes<CommandAttribute>())
            {
                if (_commandHandlers.ContainsKey(cmdAttr.Command))
                {
                    _logger?.LogWarning(
                        "Duplicate command registration: /{Command} in method {Method}. Previous registration will be overwritten.",
                        cmdAttr.Command,
                        method.Name);
                }

                _commandHandlers[cmdAttr.Command] = handlerInfo;
            }

            foreach (var cbAttr in method.GetCustomAttributes<CallbackQueryAttribute>())
            {
                _callbackHandlers.Add((cbAttr, handlerInfo));
            }

            var textAttr = method.GetCustomAttribute<TextAttribute>();
            if (textAttr != null)
            {
                _textHandlers.Add((textAttr, handlerInfo));
            }

            var chatMemberAttr = method.GetCustomAttribute<ChatMemberAttribute>();
            if (chatMemberAttr != null)
            {
                _chatMemberHandlers.Add(handlerInfo);
            }

            var contactAttr = method.GetCustomAttribute<ContactAttribute>();
            if (contactAttr != null)
            {
                _contactHandlers.Add(handlerInfo);
            }
        }
    }

    public async Task<bool> RouteAsync(Context context)
    {
        var index = -1;

        async Task<bool> Next()
        {
            index++;

            if (index < _middlewares.Count)
            {
                bool handled = false;

                await _middlewares[index].InvokeAsync(
                    context,
                    async () =>
                    {
                        handled = await Next();
                    }
                );

                return handled;
            }

            return await RouteInternalAsync(context);
        }

        return await Next();
    }

    public async Task<bool> RouteInternalAsync(Context context)
    {
        var update = context.Update;

        if (update.Type == UpdateType.Message && update.Message?.Text?.StartsWith("/") == true)
        {
            var commandText = update.Message.Text.Split(' ')[0].TrimStart('/');
            var atIndex = commandText.IndexOf('@');
            if (atIndex > 0)
            {
                commandText = commandText[..atIndex];
            }

            if (_commandHandlers.TryGetValue(commandText, out var cmdHandler))
            {
                await InvokeHandlerAsync(cmdHandler, context);
                return true;
            }
        }

        if (update.Type == UpdateType.CallbackQuery)
        {
            var callbackData = update.CallbackQuery?.Data;
            foreach (var (attr, handler) in _callbackHandlers)
            {
                if (attr.Matches(callbackData))
                {
                    await InvokeHandlerAsync(handler, context);
                    return true;
                }
            }
        }

        if (update.Type == UpdateType.Message && update.Message?.Contact != null)
        {
            foreach (var handler in _contactHandlers)
            {
                await InvokeHandlerAsync(handler, context);
                return true;
            }
        }

        if (update.Type == UpdateType.Message && !string.IsNullOrEmpty(update.Message?.Text)
            && !update.Message.Text.StartsWith("/"))
        {
            foreach (var (attr, handler) in _textHandlers.OrderBy(h => h.Attr.Priority))
            {
                if (attr.State != null && !string.Equals(context.Session?.State, attr.State, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsTextMatch(attr, update.Message!.Text!))
                    continue;

                await InvokeHandlerAsync(handler, context);
                return true;
            }
        }

        if (update.Type == UpdateType.ChatMember || update.Type == UpdateType.MyChatMember)
        {
            foreach (var handler in _chatMemberHandlers)
            {
                await InvokeHandlerAsync(handler, context);
                return true;
            }
        }

        return false;
    }

    private async Task InvokeHandlerAsync(HandlerInfo handler, Context context)
    {
        var botClient = context.BotClient;
        var update = context.Update;
        var cancellationToken = context.CancellationToken;
        
        IServiceScope? scope = null;
        IServiceProvider? serviceProvider = null;
        
        if (context.ServiceProvider != null)
        {
            var scopeFactory = context.ServiceProvider.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
            if (scopeFactory != null)
            {
                scope = scopeFactory.CreateScope();
                serviceProvider = scope.ServiceProvider;
            }
            else
            {
                serviceProvider = context.ServiceProvider;
            }
        }

        var parameters = handler.Method.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (param.ParameterType == typeof(ITelegramBotClient))
                args[i] = botClient;
            else if (param.ParameterType == typeof(Update))
                args[i] = update;
            else if (param.ParameterType == typeof(CancellationToken))
                args[i] = cancellationToken;
            else if (param.ParameterType == typeof(Message))
                args[i] = update.Message;
            else if (param.ParameterType == typeof(CallbackQuery))
                args[i] = update.CallbackQuery;
            else if (param.ParameterType == typeof(Context))
                args[i] = context;
            else if (param.ParameterType == typeof(ChatMemberUpdated))
                args[i] = update.ChatMember ?? update.MyChatMember;
            else if (param.ParameterType == typeof(Contact))
                args[i] = update.Message?.Contact;
            else
            {
                object? resolvedValue = null;

                if (serviceProvider != null)
                {
                    try
                    {
                        resolvedValue = serviceProvider.GetService(param.ParameterType);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex,
                            "Failed to resolve parameter {ParameterName} of type {ParameterType} for handler {HandlerMethod}",
                            param.Name,
                            param.ParameterType.Name,
                            handler.Method.Name);
                    }
                }

                if (resolvedValue == null && !param.HasDefaultValue && !IsNullable(param.ParameterType))
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve required parameter '{param.Name}' of type '{param.ParameterType.Name}' " +
                        $"for handler method '{handler.Method.DeclaringType?.Name}.{handler.Method.Name}'. " +
                        $"Register the service in DI or make the parameter optional.");
                }

                args[i] = resolvedValue ?? (param.HasDefaultValue ? param.DefaultValue : null);
            }
        }

        try
        {
            var result = handler.Method.Invoke(handler.Instance, args);

            if (result is Task task)
            {
                await task;
            }
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    private static bool IsTextMatch(TextAttribute attr, string text)
    {
        if (attr.Pattern != null)
            return string.Equals(text, attr.Pattern, StringComparison.OrdinalIgnoreCase);

        if (attr.Contains != null)
            return text.Contains(attr.Contains, StringComparison.OrdinalIgnoreCase);

        if (attr.Regex != null)
            return Regex.IsMatch(text, attr.Regex, RegexOptions.IgnoreCase);

        return true;
    }
}