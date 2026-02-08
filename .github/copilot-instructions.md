# Copilot Instructions for YTBlog-Pilot

## Build, Test, and Lint Commands

- **Build the solution:**
  ```bash
  dotnet build
  ```
- **Build for release:**
  ```bash
  dotnet build --configuration Release
  ```
- **Run all tests:**
  ```bash
  dotnet test
  ```
- **Run a single test (by filter):**
  ```bash
  dotnet test --filter FullyQualifiedName~VideoToBlog.Tests.BlogPostServiceTests.GenerateBlogPostAsync_WithValidVideoInfo_ReturnsBlogPostWithCorrectTitle
  ```
- **Run the CLI locally (interactive mode):**
  ```bash
  dotnet run --project VideoToBlog/VideoToBlog.csproj
  ```
- **Run the CLI locally (command-line mode):**
  ```bash
  dotnet run --project VideoToBlog/VideoToBlog.csproj -- <youtube-url> [options]
  ```
- **Linting:**
  - Formatting and naming are enforced via `.editorconfig`.
  - No separate linter needed; rules are applied during build.

## High-Level Architecture

### Solution Structure
- **VideoToBlog/** - Main CLI project built with Spectre.Console.Cli
  - `Program.cs` - Entry point, routes to InteractiveCommand (no args) or ConvertCommand (with args)
  - `Commands/` - CLI command definitions
    - `InteractiveCommand` - Guided interactive mode with prompts
    - `ConvertCommand` - Traditional command-line mode
  - `Services/` - Core business logic (YouTube, BlogPost, Template, Output)
  - `Models/` - Data models (VideoInfo, BlogPost as records)
  - `Configuration/` - AppSettings model
  - `Exceptions/` - Custom exceptions (YouTubeUrlException, CopilotException)
- **VideoToBlog.Tests/** - xUnit test project with Moq and FluentAssertions
- **templates/** - Markdown templates with `{{PLACEHOLDER}}` syntax
- **output/** - Default directory for generated blog posts
- **.cache/** - Transcript cache (SHA256 hash keys, 7-day TTL)

### Dual Mode Operation
The app has **two distinct modes** determined by presence of command-line arguments:

1. **Interactive Mode** (no arguments):
   - Launches `InteractiveCommand`
   - Prompts user for:
     - AI model selection (Claude Sonnet/Haiku/Opus, GPT-5.x, Gemini)
     - YouTube URL(s) - single, multiple, or playlist URLs
     - Template name
     - Output directory
     - HTML generation option
     - File overwriting behavior
   - Displays help text BEFORE prompting for input
   - Shows progress bars and status updates
   - Confirms model usage before and after generation

2. **Command-Line Mode** (with arguments):
   - Launches `ConvertCommand` with traditional CLI args
   - Single video URL only
   - All options via flags (--template, --output, --html, --force, --verbose)
   - Backwards compatible with original design

### Core Data Flow

#### Single Video Processing
1. **CLI Entry** (`ConvertCommand` or `InteractiveCommand`) validates input
2. **YouTubeService** validates URL → extracts video ID → fetches metadata/transcript → caches result
3. **BlogPostService** chunks transcript (1500 words) → calls `copilot` CLI → generates content
4. **TemplateService** loads template → substitutes `{{PLACEHOLDERS}}` → adds YAML frontmatter
5. **OutputService** generates filename (YYYY-MM-DD-slug.md) → writes Markdown/HTML → handles conflicts

#### Multi-Video/Playlist Processing
1. **InteractiveCommand** accepts multiple URLs (comma/space separated) or playlist URLs
2. **YouTubeService.GetPlaylistVideoUrlsAsync()** expands playlists to individual video URLs
3. For each video: fetch VideoInfo with transcript (parallelized with progress tracking)
4. **BlogPostService.GenerateBlogPostFromMultipleVideosAsync()** combines:
   - All transcripts into single input
   - Creates source video list section
   - Generates comprehensive title from common themes
   - Aggregates tags from all videos
   - Produces unified blog post

### Key Dependencies
- **YoutubeExplode (6.5.6)**: Fetches video metadata, playlists, and closed captions (no API key needed)
- **Polly (8.5.0)**: Retry logic with exponential backoff for YouTube API calls
- **Spectre.Console (0.49.1)**: CLI UI with progress spinners, prompts, and status messages
- **Markdig (0.37.0)**: Markdown to HTML conversion with advanced extensions
- **YamlDotNet (16.3.0)**: YAML frontmatter serialization

### External Process Integration
- **GitHub Copilot CLI**: BlogPostService spawns `copilot` process (standalone command)
  - **IMPORTANT**: Uses new standalone `copilot` command (NOT `gh copilot`)
  - Command format: `copilot -p "prompt" --silent --allow-all --model {model}`
  - Supports model selection via `--model` flag (AppSettings.SelectedModel)
  - Writes prompt to process arguments, reads generated content from stdout
  - Requires `copilot` CLI authenticated with `copilot login`
  - Throws `CopilotException` if process fails or returns non-zero exit code
  - Debug logging shows actual command executed (first 100 chars)

## Key Conventions

### C# Patterns
- **File-scoped namespaces**: `namespace VideoToBlog.Services;` (no braces)
- **Records for immutability**: `VideoInfo` and `BlogPost` use `record` with `required` properties
- **Partial classes with regex**: `YouTubeService` uses `[GeneratedRegex]` for URL validation (both video and playlist patterns)
- **Collection expressions**: Use `[]` for empty collections (e.g., `Tags = []`)
- **Raw string literals**: Use `"""` for multi-line strings (prompts, HTML templates)
- **Pattern matching**: Prefer `is null`/`is not null` over `== null`
- **Nullable reference types**: Enabled project-wide, trust annotations
- **Top-level statements**: Program.cs uses top-level statements (no Main method)

### Service Layer Patterns
- **Constructor injection**: Services receive `AppSettings` in constructor
- **Async all the way**: All service methods are async and accept `CancellationToken`
- **Resilience**: YouTubeService uses Polly `ResiliencePipeline` for retries
- **Caching**: SHA256 hash of URL → JSON file in `.cache/` directory
- **Chunking**: BlogPostService splits transcripts into configurable chunks (default 1500 words)
- **Static methods for stateless operations**: `YouTubeService.GetPlaylistVideoUrlsAsync()` is static

### Interactive Mode Patterns
- **Spectre.Console prompts**:
  - `SelectionPrompt<string>` for model selection (with descriptions)
  - `TextPrompt<string>` with validation for URL input
  - `ConfirmationPrompt` for yes/no questions
  - `Progress` API for multi-video fetching
  - `Status` API with spinner for AI generation
  - `FigletText` for ASCII art banner
- **Help text first**: Show supported formats BEFORE prompting for input
- **User feedback**:
  - Display selected model before processing
  - Show model used in final summary
  - Preview videos to be processed
  - Require confirmation before expensive operations

### Multi-Video Processing
- **URL parsing**: Splits on commas, spaces, semicolons, newlines
- **Playlist detection**: Regex match for `playlist?list=`
- **Expansion strategy**: Playlist URLs converted to individual video URLs before processing
- **Content merging**:
  - Combine all transcripts with section markers
  - Extract common theme from all titles (word frequency analysis)
  - Aggregate tags from all videos (up to 10)
  - Generate source video list with metadata
  - Use first video's URL as primary reference
- **Error handling**: Individual video failures don't stop batch processing

### Configuration
- **appsettings.json**: Primary config with `AppSettings` section
  - `CacheDirectory`, `CacheTtlDays`, `MaxTranscriptLength`, `ChunkSize`
  - `DefaultOutputDirectory`, `DefaultTemplate`, `MaxRetries`, `RetryDelaySeconds`
  - `CopilotTimeout` (300 seconds default)
  - `SelectedModel` (set at runtime by InteractiveCommand)
- **Environment overrides**: `appsettings.Development.json` for dev settings
- **No secrets in config**: Copilot CLI handles authentication externally

### Template System
- **Location**: `templates/` directory (e.g., `default.md`, `custom.md`)
- **Placeholders**: `{{TITLE}}`, `{{DESCRIPTION}}`, `{{DATE}}`, `{{TAGS}}`, `{{READING_TIME}}`, `{{VIDEO_URL}}`, `{{CONTENT}}`
- **YAML frontmatter**: Auto-generated with underscored keys (e.g., `reading_time: 5 min read`)
- **Fallback**: TemplateService generates default if custom template missing

### Testing Conventions
- **Test class naming**: `{ServiceName}Tests.cs` (e.g., `YouTubeServiceTests.cs`)
- **Test method naming**: `MethodName_Condition_ExpectedBehavior`
- **No AAA comments**: Omit "Arrange", "Act", "Assert" comments
- **FluentAssertions**: Use `.Should()` syntax for all assertions
- **Moq**: Mock external dependencies (not used heavily since services are concrete)
- **Theory data**: Use `[InlineData]` for parameterized tests (e.g., URL validation)

### Error Handling
- **Custom exceptions**: `YouTubeUrlException` for invalid URLs, `CopilotException` for GitHub CLI failures
- **Validation at boundaries**: URL validation in YouTubeService, not in command
- **Graceful degradation**: BlogPostService catches Copilot errors and throws with context
- **User-friendly messages**: CLI displays clean error messages, verbose mode shows stack traces
- **Multi-video tolerance**: Individual video failures logged but don't abort entire batch

### Output Formatting
- **Filename generation**: `YYYY-MM-DD-slug-from-title.md` (sanitized, lowercase)
- **Conflict resolution**: Auto-increment (`-1`, `-2`) if file exists, unless `--force`
- **HTML generation**: Uses Markdig pipeline with embedded CSS, self-contained
- **Progress indicators**: Spectre.Console Status API with Dots spinner and green color
- **Model confirmation**: Display selected model during and after generation

### Code Style (from .editorconfig)
- **Newline before brace**: Always insert newline before `{` in code blocks
- **Interface naming**: Must start with `I` (enforced by analyzer)
- **XML docs**: Required for all public APIs, include `<summary>` and examples when applicable
- **nameof**: Use `nameof(variable)` instead of string literals for member names
