# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[1.0.8]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.8
[1.0.7]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.7
[1.0.6]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.6
[1.0.5]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.5
[1.0.4]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.4
[1.0.0]: https://github.com/TimurbekDev/Kippo/releases/tag/v1.0.0
