# Contributing to YTBlog-Pilot

Thank you for considering contributing to YTBlog-Pilot! This document provides guidelines and instructions for contributing.

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)
- [GitHub Copilot CLI](https://github.com/github/gh-copilot) (for testing AI features)
- A code editor (VS Code, Rider, or Visual Studio recommended)

### Setting Up Development Environment

1. **Fork the repository**
   ```bash
   # Click "Fork" on GitHub, then clone your fork
   git clone https://github.com/YOUR_USERNAME/ytblog-pilot.git
   cd ytblog-pilot
   ```

2. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/ORIGINAL_OWNER/ytblog-pilot.git
   ```

3. **Build and test**
   ```bash
   dotnet build
   dotnet test
   ```

4. **Run locally**
   ```bash
   dotnet run --project VideoToBlog/VideoToBlog.csproj
   ```

## 📋 Development Workflow

### 1. Create a Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/bug-description
```

**Branch naming conventions:**
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring
- `test/` - Test additions or updates

### 2. Make Your Changes

- Follow the existing code style (see Code Style section)
- Write tests for new features
- Update documentation as needed
- Keep commits focused and atomic

### 3. Test Your Changes

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter FullyQualifiedName~YourTestName

# Build in release mode
dotnet build --configuration Release

# Test the CLI locally
dotnet run --project VideoToBlog/VideoToBlog.csproj
```

### 4. Commit Your Changes

```bash
git add .
git commit -m "feat: add support for custom AI models"
```

**Commit message format:**
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `test:` - Test additions/updates
- `refactor:` - Code refactoring
- `style:` - Code style changes
- `chore:` - Build/config changes

### 5. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub with:
- Clear title describing the change
- Description of what changed and why
- Reference any related issues

## 🎨 Code Style

### C# Conventions

- **File-scoped namespaces**: Use `namespace VideoToBlog.Services;` (no braces)
- **Records for immutability**: Use `record` for data models
- **Async all the way**: All service methods should be async
- **Nullable reference types**: Enabled project-wide
- **Pattern matching**: Prefer `is null`/`is not null` over `== null`
- **Collection expressions**: Use `[]` for empty collections

### Example

```csharp
namespace VideoToBlog.Services;

public class MyService
{
    private readonly AppSettings _settings;

    public MyService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<Result> ProcessAsync(string input, CancellationToken cancellationToken = default)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        // Implementation
        return new Result 
        { 
            Success = true, 
            Tags = [] 
        };
    }
}
```

### Testing Conventions

- **Test class naming**: `{ServiceName}Tests.cs`
- **Test method naming**: `MethodName_Condition_ExpectedBehavior`
- **Use FluentAssertions**: `.Should()` syntax for assertions
- **No AAA comments**: Code should be self-documenting

```csharp
[Fact]
public async Task GenerateBlogPostAsync_WithValidVideoInfo_ReturnsBlogPostWithCorrectTitle()
{
    // Arrange
    var service = new BlogPostService(settings);
    var videoInfo = new VideoInfo { Title = "Test Video", Transcript = "..." };

    // Act
    var result = await service.GenerateBlogPostAsync(videoInfo);

    // Assert
    result.Title.Should().Be("Test Video");
}
```

## 📝 Documentation

- Update README.md if you add new features or change behavior
- Add XML documentation comments to public APIs
- Update `.github/copilot-instructions.md` for architectural changes
- Include examples in documentation

## 🐛 Reporting Bugs

### Before Submitting

1. Check existing issues to avoid duplicates
2. Verify the bug exists in the latest version
3. Collect relevant information

### Bug Report Template

```markdown
**Describe the bug**
A clear description of what the bug is.

**To Reproduce**
Steps to reproduce:
1. Run `ytblog-pilot ...`
2. Enter ...
3. See error

**Expected behavior**
What you expected to happen.

**Actual behavior**
What actually happened.

**Environment:**
- OS: [e.g., Ubuntu 22.04, Windows 11, macOS 14]
- .NET version: [e.g., 10.0.2]
- YTBlog-Pilot version: [e.g., 1.0.0]

**Additional context**
Any other relevant information.
```

## ✨ Requesting Features

### Feature Request Template

```markdown
**Is your feature request related to a problem?**
A clear description of the problem.

**Describe the solution you'd like**
How you envision the feature working.

**Describe alternatives you've considered**
Other solutions you've thought about.

**Additional context**
Mockups, examples, or related features.
```

## 🔍 Code Review Process

1. **Automated Checks**: All tests must pass
2. **Code Review**: At least one maintainer approval required
3. **Documentation**: Ensure all changes are documented
4. **Clean History**: Squash commits if needed

## 📜 License

By contributing, you agree that your contributions will be licensed under the MIT License.

## 🙏 Recognition

Contributors will be recognized in:
- GitHub's contributor graph
- Release notes for significant contributions
- This CONTRIBUTING.md file (optional hall of fame)

## 💬 Questions?

- Open a [GitHub Discussion](https://github.com/yourusername/ytblog-pilot/discussions)
- Check `.github/copilot-instructions.md` for architecture details
- Review existing code for examples

---

Thank you for contributing to YTBlog-Pilot! 🎉
