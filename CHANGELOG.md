# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.0.6]: https://github.com/TimurbekDev/KippoGramm/releases/tag/v1.0.6
[1.0.5]: https://github.com/TimurbekDev/KippoGramm/releases/tag/v1.0.5
[1.0.4]: https://github.com/TimurbekDev/KippoGramm/releases/tag/v1.0.4
[1.0.0]: https://github.com/TimurbekDev/KippoGramm/releases/tag/v1.0.0
