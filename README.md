# 🤖 Kippo

[![NuGet](https://img.shields.io/nuget/v/Kippo.svg)](https://www.nuget.org/packages/Kippo/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)

A lightweight, attribute-based framework for building Telegram bots in .NET with session management, middleware support, and intuitive routing.

## 📦 Installation

```bash
dotnet add package Kippo
```

## 🚀 Quick Example

```csharp
// Description is auto-registered to Telegram's command menu on startup
[Command("start", Description = "Start the bot")]
public async Task Start(Context context)
{
    await context.Reply("Hello! 👋");
}

[Command("register")]
public async Task Register(Context context)
{
    context.Session.SetState("awaiting_name");
    await context.Reply("What's your name?");
}

[Text(State = "awaiting_name")]
public async Task HandleName(Context context)
{
    var name = context.Message.Text;
    context.Session.Set("name", name);   // marks the session dirty → persisted
    context.Session.ClearState();
    await context.Reply($"Nice to meet you, {name}!");
}

// Typed callback routing — placeholders are parsed and bound by name
[CallbackQuery("product:{id}:{action}")]
public async Task OnProduct(Context context, int id, string action)
{
    await context.Callback.Answer();
    await context.Reply($"Product {id} → {action}");
}

// Catch-all for updates that matched no other handler (e.g. unknown commands)
[Fallback]
public async Task Unknown(Context context)
{
    await context.Reply("I don't understand. Try /help");
}
```

## ✨ Key Features

- 🎯 **Attribute-based routing** - `[Command]`, `[Text]`, `[CallbackQuery]`, `[ChatMember]`, `[Contact]`, `[Fallback]`
- 📋 **Auto command menu** - `[Command]` descriptions are synced to Telegram's `/` menu on startup via `SetMyCommands`
- 🔗 **Typed callback data** - `{placeholder}` templates parsed & bound to typed method params
- 💾 **Session management** - Track user state and data across conversations, with typed state helpers (`SetState`/`InState`) and configurable TTL/LRU eviction
- 🔌 **Middleware pipeline** - Add logging, auth, rate limiting, and more
- ⌨️ **Keyboard builders** - Fluent API for reply and inline keyboards
- 💉 **Service injection** - Full ASP.NET Core DI support
- 🚀 **Production ready** - Thread-safe, optimized for performance

## 📖 Full Documentation

🌐 **Complete Guides & API Reference:** [https://kippo.uz](https://kippo.uz)

- Installation & Setup
- Tutorial & Examples
- API Reference
- Best Practices
- Advanced Usage

## 🤝 Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) and the [Code of Conduct](CODE_OF_CONDUCT.md).

## 📝 Changelog

See [CHANGELOG.md](CHANGELOG.md) for release history.

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.
