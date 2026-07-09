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
- 🎬 **Scenes & conversations** - Write multi-step dialogs as linear code with `await ctx.Ask()` — resumable, typed, and testable
- 🔌 **Middleware pipeline** - Add logging, auth, rate limiting, and more
- ⌨️ **Keyboard builders** - Fluent API for reply and inline keyboards
- 💉 **Service injection** - Full ASP.NET Core DI support
- 🧪 **Testable** - Drive your bot with fake updates in unit tests, no token or network
- 🚦 **Flood control** - Automatic retry on Telegram `429` + optional per-chat throttling
- 🗄️ **Large callback payloads** - Attach arbitrarily big typed data to buttons, past the 64-byte limit
- 🚀 **Production ready** - Thread-safe, optimized for performance

## 🎬 Scenes & Conversations

Write multi-step dialogs as plain sequential code instead of scattering handlers across a hand-rolled
state machine. Each `await ctx.Ask(...)` sends a prompt and returns the user's next reply; progress is
persisted to the session between messages and resumes automatically.

```csharp
[Command("signup")]
public Task Start(Context c)
{
    c.EnterScene("signup");           // sends the first prompt, then runs the scene
    return Task.CompletedTask;
}

[Scene("signup")]
public async Task Signup(SceneContext ctx)
{
    var name = await ctx.Ask("What's your name?");
    await ctx.Reply($"Hi {name}! 👋");

    var age = await ctx.Ask<int>("How old are you?", retry: "Please send a number 🙂");

    await ctx.Reply($"All set, {name} — registered at age {age}. ✅");
}
```

`Ask<T>` parses and validates the reply (int, Guid, enum, …) and re-asks on invalid input. Scenes
support DI, only intercept plain text (so `/cancel` always works via `context.ExitScene()`), and are
fully driveable with `TestBot`.

## 🧪 Testing

Test handlers end-to-end without a bot token or network. `TestBot<T>` wires the real router,
middleware and session store around a fake client that records everything the bot sends.

```csharp
var bot = new TestBot<MyHandler>();

await bot.SendCommand("start");
Assert.Equal("Hello! 👋", bot.LastReply?.Text);

await bot.SendCommand("register");
await bot.SendText("Timur");
Assert.Null(bot.Session.State);
Assert.Equal("Nice to meet you, Timur!", bot.LastReply?.Text);

await bot.TapButton("product:42:buy");            // simulate an inline button tap
Assert.Contains(bot.Client.SentOf<AnswerCallbackQueryRequest>(), _ => true);
```

Register the fakes/services your handler resolves via the constructor:
`new TestBot<MyHandler>(services => services.AddSingleton<IClock>(fakeClock))`.

## 🚦 Flood control

Opt in when calling `AddKippo` to survive Telegram's rate limits transparently. Outbound requests
that fail with `429 Too Many Requests` are retried honoring the server's `retry_after`; set
`MinIntervalPerChat` to also space out messages per chat.

```csharp
builder.Services.AddKippo<MyHandler>(
    builder.Configuration,
    configureFloodControl: opt =>
    {
        opt.MaxRetries = 3;
        opt.MinIntervalPerChat = TimeSpan.FromSeconds(1);
    });
```

## 🗄️ Large callback payloads (the 64-byte problem)

Telegram caps `callback_data` at 64 bytes. Build the keyboard with `context.Inline()` and attach a
typed payload of any size via `.Payload(...)` — Kippo stores it in a callback vault and puts only a
short token on the button. The payload is rebound to a typed handler parameter on tap.

```csharp
[Command("buy")]
public Task Buy(Context c) =>
    c.Reply("Confirm your order:", c.Inline()
        .Payload("✅ Buy", "order:buy", new Order(Id: 42, Coupon: "SUMMER-2026-EXTRA-LONG", Qty: 3))
        .Build());

[CallbackQuery("order:buy")]
public async Task OnBuy(Context c, Order order)   // payload bound by type
{
    await c.Callback.Answer();
    await c.Reply($"Ordered {order.Qty}× #{order.Id} ({order.Coupon})");
}
```

Swap the default in-memory vault for a distributed `ICallbackStore` (Redis, DB) to keep tokens valid
across restarts and multiple instances.

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
