using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using VideoToBlog.Configuration;
using VideoToBlog.Services;

namespace VideoToBlog.Commands;

/// <summary>
/// CLI command for converting YouTube videos to blog posts.
/// </summary>
public class ConvertCommand : AsyncCommand<ConvertCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("YouTube video URL")]
        [CommandArgument(0, "<youtube-url>")]
        public required string YoutubeUrl { get; init; }

        [Description("Template name (default: 'default')")]
        [CommandOption("-t|--template")]
        [DefaultValue("default")]
        public string Template { get; init; } = "default";

        [Description("Output directory (default: './output')")]
        [CommandOption("-o|--output")]
        [DefaultValue("./output")]
        public string OutputDirectory { get; init; } = "./output";

        [Description("Generate HTML output in addition to Markdown")]
        [CommandOption("--html")]
        [DefaultValue(false)]
        public bool GenerateHtml { get; init; }

        [Description("Overwrite existing files")]
        [CommandOption("-f|--force")]
        [DefaultValue(false)]
        public bool Force { get; init; }

        [Description("Enable verbose logging")]
        [CommandOption("-v|--verbose")]
        [DefaultValue(false)]
        public bool Verbose { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var appSettings = LoadConfiguration();
            appSettings.DefaultOutputDirectory = settings.OutputDirectory;

            await AnsiConsole.Status()
                .StartAsync("Processing video...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    ctx.Status("Fetching video information...");
                    var youtubeService = new YouTubeService(appSettings);
                    var videoInfo = await youtubeService.GetVideoInfoAsync(settings.YoutubeUrl);

                    if (settings.Verbose)
                    {
                        AnsiConsole.MarkupLine($"[dim]Title: {videoInfo.Title}[/]");
                        AnsiConsole.MarkupLine($"[dim]Duration: {videoInfo.Duration}[/]");
                        AnsiConsole.MarkupLine($"[dim]Transcript length: {videoInfo.Transcript.Split(' ').Length} words[/]");
                    }

                    ctx.Status("Generating blog post with GitHub Copilot...");
                    var blogPostService = new BlogPostService(appSettings);
                    var blogPost = await blogPostService.GenerateBlogPostAsync(videoInfo);

                    if (settings.Verbose)
                    {
                        AnsiConsole.MarkupLine($"[dim]Reading time: {blogPost.ReadingTime} minutes[/]");
                        AnsiConsole.MarkupLine($"[dim]Tags: {string.Join(", ", blogPost.Tags)}[/]");
                    }

                    ctx.Status("Applying template...");
                    var templateService = new TemplateService("templates");
                    var finalContent = templateService.ApplyTemplate(blogPost, settings.Template);

                    ctx.Status("Writing output files...");
                    var outputService = new OutputService(settings.OutputDirectory);

                    if (settings.GenerateHtml)
                    {
                        var (markdownPath, htmlPath) = await outputService.WriteMarkdownAndHtmlAsync(
                            finalContent, 
                            blogPost.Title, 
                            settings.Force);

                        AnsiConsole.MarkupLine($"[green]✓[/] Blog post generated successfully!");
                        AnsiConsole.MarkupLine($"[blue]Markdown:[/] {markdownPath}");
                        AnsiConsole.MarkupLine($"[blue]HTML:[/] {htmlPath}");
                    }
                    else
                    {
                        var filePath = await outputService.WriteMarkdownAsync(
                            finalContent, 
                            blogPost.Title, 
                            settings.Force);

                        AnsiConsole.MarkupLine($"[green]✓[/] Blog post generated successfully!");
                        AnsiConsole.MarkupLine($"[blue]File:[/] {filePath}");
                    }
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            
            if (settings.Verbose)
            {
                AnsiConsole.WriteException(ex);
            }

            return 1;
        }
    }

    private static AppSettings LoadConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var settings = new AppSettings();
        configuration.GetSection("AppSettings").Bind(settings);

        return settings;
    }
}
