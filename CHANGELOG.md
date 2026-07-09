# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.5] - 2026-07-09

### Changed
- Updated `Telegram.Bot` dependency to `22.10.1.1`
- Enriched NuGet package metadata (title, description, and tags) for discoverability

### Packaging
- Enabled SourceLink (`Microsoft.SourceLink.GitHub`), symbol packages (`.snupkg`), and deterministic builds for a source-debuggable, verifiable package

## [1.1.4] - 2026-07-09

### Added
- Webhook hosting: `app.MapKippoWebhook("/bot", secretToken)` receives updates over HTTP and runs them through the same router, middleware, session and scene pipeline as long polling. Disable polling with `AddKippo(..., useLongPolling: false)`. The secret token is validated against Telegram's `X-Telegram-Bot-Api-Secret-Token` header (401 on mismatch); malformed bodies return 400. When `Kippo:WebhookUrl` is configured, Kippo registers the webhook with Telegram (`setWebhook`) and the command menu on startup

### Changed
- Kippo now references the `Microsoft.AspNetCore.App` shared framework (its documented host is already an ASP.NET Core `WebApplication`), replacing the standalone `Microsoft.Extensions.Hosting`/`Logging` package references

## [1.1.3] - 2026-07-09

### Added
- Scenes & conversations: mark a method with `[Scene("name")]` and write multi-step dialogs as linear code — `await ctx.Ask(prompt)` sends a prompt and returns the user's next reply; `await ctx.Ask<T>(prompt, retry)` parses and validates it (int, Guid, enum, …), re-asking on invalid input. Enter from any handler with `context.EnterScene("name")`; exit with `context.ExitScene()` (`context.InScene` reports status). Progress persists to the session between messages and resumes automatically, so scenes survive restarts with a persistent `ISessionStore`. Scenes support DI, only intercept plain text (commands remain reachable), and are fully driveable with `TestBot`

## [1.1.2] - 2026-07-09

### Added
- Testing harness (`Kippo.Testing`): `TestBot<THandler>` drives a handler through the real router, middleware pipeline and session store with synthetic updates (`SendText`, `SendCommand`, `TapButton`, `SendContact`), backed by a `RecordingBotClient` that captures every outbound request — no bot token or network required
- Flood control: opt-in `ThrottlingBotClient` (via `AddKippo(..., configureFloodControl: ...)`) transparently retries requests that fail with `429 Too Many Requests`, honoring the server-supplied `retry_after`, and can throttle sends per chat (`MinIntervalPerChat`)
- Callback-data vault: `context.Inline().Payload(text, route, payload)` attaches arbitrarily large typed payloads to inline buttons, sidestepping Telegram's 64-byte `callback_data` limit by storing the payload behind a short token (`ICallbackStore`, in-memory by default) and rebinding it to a typed handler parameter on callback

## [1.1.1] - 2026-07-08

### Added
- Fallback handler: mark one method with `[Fallback]` to catch updates that matched no command, callback, text, contact or chat-member handler — ideal for replying to unknown commands. Runs inside the middleware pipeline after all other routes are checked
- Automatic command menu: commands declaring a `Description` on `[Command]` are registered to Telegram's `/` command menu on startup via `SetMyCommands`, capped at Telegram's 100-command limit and resilient to network failures

## [1.1.0] - 2026-07-08

### Added
- Session state helper API: `SetState`/`ClearState`/`InState` plus enum-typed overloads (`SetState<TEnum>`, `GetState<TEnum>`, `InState<TEnum>`) for type-safe FSM states
- `SessionExtensions.Remove(key)` to remove a session data entry
- Session eviction via `SessionOptions` (`Ttl` sliding expiration, `MaxSessions` LRU cap, `SweepInterval`) configurable through `AddKippo(config, opt => ...)` — prevents unbounded in-memory session growth

### Changed
- Session persistence now uses dirty tracking: `SaveAsync` runs only when the session was actually mutated, cutting redundant writes (especially for external stores)
- `SessionMiddleware` serializes concurrent updates per chat with striped locks, preventing lost updates when the same user sends updates in parallel

## [1.0.9] - 2026-07-07

### Added
- Typed callback data routing: `[CallbackQuery]` patterns now support `{name}` placeholders (e.g. `product:{id}:{action}`) that are parsed and bound to handler method parameters by name, with automatic type conversion (int, long, Guid, enum, etc.)

## [1.0.8] - 2026-06-29

### Added
- Contact handling support via `[Contact]` attribute for `contact` messages
- .NET 10 target framework support (`net8.0;net9.0;net10.0`)

## [1.0.7] - 2026-06-22

### Added
- Chat member update handlers via `[ChatMember]` attribute

### Changed
- Refactored and simplified README structure

## [1.0.6] - 2026-02-12

### Changed
- Simplified README files for better user experience
- Improved documentation structure directing users to website
- Cleaner project presentation and consistent formatting
- Enhanced developer onboarding experience

## [1.0.5] - 2026-02-12

### Changed
- Updated documentation and README for better clarity
- Minor improvements and bug fixes

## [1.0.4] - 2026-02-01

### Added
- Thread-safe sessions with ConcurrentDictionary for safe concurrent access
- Automatic service injection in handler methods
- Full support for scoped services (DbContext, EF Core, etc.)
- Integrated ILogger support throughout the framework
- Optimized network usage with AllowedUpdates configuration
- Better error messages and null-safety improvements

### Breaking Changes
- ISessionStore interface now requires DeleteAsync method

## [1.0.0] - 2026-01-20

### Added
- Initial release of Kippo framework
- Attribute-based routing with `[Command]`, `[Text]`, and `[CallbackQuery]` attributes
- Built-in session management with `ISessionStore` interface
- In-memory session storage implementation
- Middleware support with `IBotMiddleware` interface
- Session middleware for automatic session management
- Fluent keyboard builders (`InlineKeyboardBuilder` and `ReplyKeyboardBuilder`)
- ASP.NET Core integration via `AddKippo` extension method
- Background service for long polling
- Context API for handling updates
- Support for state-based message routing
- Extension methods for easy configuration

### Features
- `BotUpdateHandler` base class for creating bot handlers
- `CommandRouter` for routing updates to appropriate handlers
- Session state tracking
- Session data dictionary for storing user information
- Automatic session loading and saving
- Reply keyboard support with resize and one-time options
- Inline keyboard support with callback data and URL buttons
- Message context with reply, edit, and delete methods
- Callback query context for handling inline keyboard interactions

[1.1.4]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.1.4
[1.1.3]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.1.3
[1.1.2]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.1.2
[1.1.1]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.1.1
[1.1.0]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.1.0
[1.0.9]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.9
[1.0.8]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.8
[1.0.7]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.7
[1.0.6]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.6
[1.0.5]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.5
[1.0.4]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.4
[1.0.0]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.0
