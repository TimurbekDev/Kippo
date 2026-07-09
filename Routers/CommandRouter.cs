using Kippo.Attribute;
using Kippo.Callbacks;
using Kippo.Contexs;
using Kippo.Middleware;
using Kippo.Scenes;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
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
    private readonly Dictionary<string, HandlerInfo> _sceneHandlers = new(StringComparer.Ordinal);
    private readonly List<IBotMiddleware> _middlewares = new();
    private readonly List<BotCommand> _botCommands = new();
    private HandlerInfo? _fallbackHandler;
    private readonly ILogger? _logger;

    /// <summary>
    /// Commands declared with a <c>Description</c> on their <c>[Command]</c> attribute,
    /// suitable for registration via <c>SetMyCommands</c>.
    /// </summary>
    public IReadOnlyList<BotCommand> BotCommands => _botCommands;

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

                if (!string.IsNullOrWhiteSpace(cmdAttr.Description))
                {
                    _botCommands.Add(new BotCommand
                    {
                        Command = cmdAttr.Command,
                        Description = cmdAttr.Description
                    });
                }
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

            var sceneAttr = method.GetCustomAttribute<SceneAttribute>();
            if (sceneAttr != null)
            {
                if (_sceneHandlers.ContainsKey(sceneAttr.Name))
                {
                    _logger?.LogWarning(
                        "Duplicate [Scene] registration: '{Scene}' in method {Method}. Previous registration will be overwritten.",
                        sceneAttr.Name,
                        method.Name);
                }

                _sceneHandlers[sceneAttr.Name] = handlerInfo;
            }

            if (method.GetCustomAttribute<FallbackAttribute>() != null)
            {
                if (_fallbackHandler != null)
                {
                    _logger?.LogWarning(
                        "Duplicate [Fallback] handler in method {Method}. Previous registration will be overwritten.",
                        method.Name);
                }

                _fallbackHandler = handlerInfo;
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
        // 1. An active scene intercepts the user's plain-text replies (commands still fall through,
        //    so /cancel and friends remain reachable as escape hatches).
        if (SceneState.IsActive(context.Session) && IsSceneInput(context))
        {
            var activeScene = SceneState.GetName(context.Session!)!;
            await RunSceneAsync(context, activeScene, context.Update.Message!.Text!);
            return true;
        }

        // 2. Normal routing.
        var handled = await DispatchAsync(context);

        // 3. A handler just called EnterScene → run the scene's opening turn (sends the first prompt).
        if (context.Items.TryGetValue(SceneState.EnterFlagKey, out var entered) && entered is string sceneName)
        {
            context.Items.Remove(SceneState.EnterFlagKey);
            await RunSceneAsync(context, sceneName, pendingInput: null);
        }

        return handled;
    }

    private static bool IsSceneInput(Context context)
    {
        var msg = context.Update.Message;
        return context.Update.Type == UpdateType.Message
            && !string.IsNullOrEmpty(msg?.Text)
            && !msg.Text.StartsWith("/");
    }

    private async Task RunSceneAsync(Context context, string sceneName, string? pendingInput)
    {
        if (context.Session is null)
            return;

        if (!_sceneHandlers.TryGetValue(sceneName, out var handler))
        {
            _logger?.LogWarning("No [Scene] handler registered for '{Scene}'. Clearing scene state.", sceneName);
            SceneState.Clear(context.Session);
            return;
        }

        var answers = SceneState.LoadAnswers(context.Session);
        var sceneContext = new SceneContext(context, answers, pendingInput);

        try
        {
            await InvokeSceneAsync(handler, context, sceneContext);
            // Reached the end without halting → the dialog is complete.
            SceneState.Clear(context.Session);
        }
        catch (SceneHaltException)
        {
            // Suspended at an unanswered Ask → persist progress for the next message.
            SceneState.SaveAnswers(context.Session, sceneContext.Answers);
        }
    }

    private async Task InvokeSceneAsync(HandlerInfo handler, Context context, SceneContext sceneContext)
    {
        IServiceScope? scope = null;
        IServiceProvider? serviceProvider = context.ServiceProvider;

        if (context.ServiceProvider?.GetService(typeof(IServiceScopeFactory)) is IServiceScopeFactory scopeFactory)
        {
            scope = scopeFactory.CreateScope();
            serviceProvider = scope.ServiceProvider;
        }

        try
        {
            var parameters = handler.Method.GetParameters();
            var args = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;
                if (type == typeof(SceneContext))
                    args[i] = sceneContext;
                else if (type == typeof(Context))
                    args[i] = context;
                else
                    args[i] = serviceProvider?.GetService(type);
            }

            if (handler.Method.Invoke(handler.Instance, args) is Task task)
                await task;
        }
        catch (TargetInvocationException tie) when (tie.InnerException is SceneHaltException halt)
        {
            // Reflection wraps the halt thrown inside the scene method — unwrap it.
            throw halt;
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private async Task<bool> DispatchAsync(Context context)
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
                if (attr.TryMatch(callbackData, out var routeValues))
                {
                    await InvokeHandlerAsync(handler, context, routeValues);
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

        if (_fallbackHandler != null)
        {
            await InvokeHandlerAsync(_fallbackHandler, context);
            return true;
        }

        return false;
    }

    private async Task InvokeHandlerAsync(
        HandlerInfo handler,
        Context context,
        IReadOnlyDictionary<string, string>? routeValues = null)
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
            else if (routeValues != null && param.Name != null
                     && routeValues.TryGetValue(param.Name, out var rawValue))
            {
                args[i] = ConvertRouteValue(rawValue, param, handler);
            }
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

                if (resolvedValue == null && IsVaultBindable(param.ParameterType)
                    && context.Items.TryGetValue(CallbackVault.PayloadItemKey, out var payloadObj)
                    && payloadObj is string payloadJson && payloadJson.Length > 0)
                {
                    try
                    {
                        resolvedValue = JsonSerializer.Deserialize(payloadJson, param.ParameterType);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex,
                            "Failed to bind vaulted callback payload to parameter {ParameterName} of type " +
                            "{ParameterType} for handler {HandlerMethod}.",
                            param.Name, param.ParameterType.Name, handler.Method.Name);
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

    private object? ConvertRouteValue(string rawValue, ParameterInfo param, HandlerInfo handler)
    {
        var targetType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;

        try
        {
            if (targetType == typeof(string))
                return rawValue;

            if (targetType.IsEnum)
                return Enum.Parse(targetType, rawValue, ignoreCase: true);

            if (targetType == typeof(Guid))
                return Guid.Parse(rawValue);

            return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex,
                "Failed to convert callback route value '{RawValue}' to type {TargetType} for parameter " +
                "'{ParameterName}' in handler {HandlerMethod}.",
                rawValue, targetType.Name, param.Name, handler.Method.Name);

            if (param.HasDefaultValue)
                return param.DefaultValue;

            if (IsNullable(param.ParameterType))
                return null;

            throw new InvalidOperationException(
                $"Cannot convert callback route value '{rawValue}' to required parameter " +
                $"'{param.Name}' of type '{targetType.Name}' for handler method " +
                $"'{handler.Method.DeclaringType?.Name}.{handler.Method.Name}'.", ex);
        }
    }

    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    // Vaulted payloads are complex objects; never hijack primitives, strings or enums which are
    // bound from route values or DI.
    private static bool IsVaultBindable(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return !underlying.IsPrimitive
            && !underlying.IsEnum
            && underlying != typeof(string)
            && underlying != typeof(Guid)
            && underlying != typeof(decimal);
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