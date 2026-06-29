# Contributing to Kippo

First off, thank you for considering contributing to Kippo! 🎉

## How Can I Contribute?

### 🐛 Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates.

When creating a bug report, include:
- **Clear title** describing the issue
- **Steps to reproduce** the behavior
- **Expected behavior** vs actual behavior
- **Code samples** if applicable
- **.NET version** and OS information

### 💡 Suggesting Features

Feature suggestions are welcome! Please:
- Check if the feature already exists or is planned
- Describe the use case and benefits
- Provide code examples if possible

### 🔧 Pull Requests

1. **Fork** the repository
2. **Clone** your fork locally
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```
4. **Make your changes** following our coding standards
5. **Test** your changes thoroughly
6. **Commit** with clear messages:
   ```bash
   git commit -m "Add: brief description of changes"
   ```
7. **Push** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```
8. **Open a Pull Request** with a clear description

## Development Setup

### Prerequisites
- .NET 8.0 SDK or later
- Your favorite IDE (VS Code, Visual Studio, Rider)

### Building the Project

```bash
git clone https://github.com/TimurbekDev/KippoGramm.git
cd KippoGramm
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Coding Standards

### C# Style Guide

- Use **PascalCase** for public members, types, and namespaces
- Use **camelCase** for private fields and local variables
- Prefix private fields with underscore: `_privateField`
- Use `var` when the type is obvious
- Use meaningful names that describe purpose
- Add XML documentation for public APIs

### Example

```csharp
/// <summary>
/// Handles the /start command.
/// </summary>
/// <param name="context">The update context.</param>
[Command("start")]
public async Task HandleStart(Context context)
{
    var userName = context.Update.Message?.From?.FirstName ?? "User";
    await context.Reply($"Welcome, {userName}!");
}
```

### Commit Messages

Use clear, descriptive commit messages:
- `Add: new feature description`
- `Fix: bug description`
- `Update: what was updated`
- `Remove: what was removed`
- `Refactor: what was refactored`

## Code of Conduct

Please be respectful and inclusive. We welcome contributors of all backgrounds and experience levels.

## Questions?

Feel free to open an issue or start a discussion if you have questions!

---

Thank you for contributing! 🚀
