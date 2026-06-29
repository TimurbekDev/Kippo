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
[Command("start")]
public async Task Start(Context context)
{
    await context.Reply("Hello! 👋");
}

[Text(State = "awaiting_name")]
public async Task HandleName(Context context)
{
    var name = context.Message.Text;
    context.Session.Data["name"] = name;
    await context.Reply($"Nice to meet you, {name}!");
}
```

## ✨ Key Features

- 🎯 **Attribute-based routing** - `[Command]`, `[Text]`, `[CallbackQuery]`, `[ChatMember]`, `[Contact]`
- 💾 **Session management** - Track user state and data across conversations
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
