using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using VideoToBlog.Configuration;
using VideoToBlog.Models;
using VideoToBlog.Services;

namespace VideoToBlog.Commands;

public class InteractiveCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            AnsiConsole.Write(new FigletText("YTBlog-Pilot").Color(Color.Blue));
            AnsiConsole.MarkupLine("[dim]Convert YouTube videos to professional blog posts with AI[/]\n");

            // 1. Select AI Model
            var model = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select [green]AI Model[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to see more models)[/]")
                    .AddChoices(new[]
                    {
                        "claude-sonnet-4.5 (Recommended)",
                        "claude-haiku-4.5 (Fast & Cheap)",
                        "claude-opus-4.6 (Premium)",
                        "gpt-5.2-codex",
                        "gpt-5.2",
                        "gpt-5.1-codex",
                        "gpt-5.1",
                        "gpt-5",
                        "gpt-4.1"
                    }));

            var selectedModel = model.Split(' ')[0]; // Extract model name without description

            // 2. Get YouTube URLs
            AnsiConsole.MarkupLine("\n[cyan]Supported inputs:[/]");
            AnsiConsole.MarkupLine("  [dim]• Single video:[/] https://www.youtube.com/watch?v=VIDEO_ID");
            AnsiConsole.MarkupLine("  [dim]• Multiple videos:[/] url1, url2, url3 (comma or space separated)");
            AnsiConsole.MarkupLine("  [dim]• Playlist:[/] https://www.youtube.com/playlist?list=PLAYLIST_ID");
            AnsiConsole.MarkupLine("  [dim]• Mixed:[/] Any combination of the above\n");

            var urlInput = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [yellow]YouTube URL(s)[/]:")
                    .PromptStyle("yellow")
                    .ValidationErrorMessage("[red]Please enter at least one valid URL[/]")
                    .Validate(input => !string.IsNullOrWhiteSpace(input)));

            // Parse URLs
            var urls = ParseUrls(urlInput);

            // 3. Get all video URLs (expand playlists)
            var videoUrls = await ExpandPlaylistsAsync(urls);

            // 4. Show preview
            AnsiConsole.MarkupLine($"\n[green]Found {videoUrls.Count} video(s) to process:[/]");
            foreach (var url in videoUrls.Take(10))
            {
                AnsiConsole.MarkupLine($"  [dim]• {url}[/]");
            }
            if (videoUrls.Count > 10)
            {
                AnsiConsole.MarkupLine($"  [dim]... and {videoUrls.Count - 10} more[/]");
            }

            // 5. Template selection
            var template = AnsiConsole.Prompt(
                new TextPrompt<string>("Template name:")
                    .DefaultValue("default")
                    .PromptStyle("cyan"));

            // 6. Output directory
            var outputDir = AnsiConsole.Prompt(
                new TextPrompt<string>("Output directory:")
                    .DefaultValue("./output")
                    .PromptStyle("cyan"));

            // 7. Additional options
            var generateHtml = AnsiConsole.Confirm("Generate HTML in addition to Markdown?", false);
            var force = AnsiConsole.Confirm("Overwrite existing files?", false);

            // 8. Confirmation
            if (!AnsiConsole.Confirm($"\n[yellow]Process {videoUrls.Count} video(s)?[/]", true))
            {
                AnsiConsole.MarkupLine("[red]Cancelled.[/]");
                return 0;
            }

            // 9. Process videos
            var appSettings = LoadConfiguration();
            appSettings.DefaultOutputDirectory = outputDir;
            appSettings.SelectedModel = selectedModel;

            var videoInfos = new List<VideoInfo>();

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Fetching video information...[/]", maxValue: videoUrls.Count);
                    var youtubeService = new YouTubeService(appSettings);

                    foreach (var url in videoUrls)
                    {
                        try
                        {
                            var videoInfo = await youtubeService.GetVideoInfoAsync(url);
                            videoInfos.Add(videoInfo);
                            task.Increment(1);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Error processing {url}: {ex.Message}[/]");
                        }
                    }
                });

            if (videoInfos.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No videos were successfully fetched.[/]");
                return 1;
            }

            // 10. Generate blog post
            await AnsiConsole.Status()
                .StartAsync("Generating blog post with AI...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    AnsiConsole.MarkupLine($"[dim]Using model: {selectedModel}[/]");

                    var blogPostService = new BlogPostService(appSettings);
                    var blogPost = videoInfos.Count == 1
                        ? await blogPostService.GenerateBlogPostAsync(videoInfos[0])
                        : await blogPostService.GenerateBlogPostFromMultipleVideosAsync(videoInfos);

                    ctx.Status("Applying template...");
                    var templateService = new TemplateService("templates");
                    var finalContent = templateService.ApplyTemplate(blogPost, template);

                    ctx.Status("Writing output files...");
                    var outputService = new OutputService(outputDir);

                    if (generateHtml)
                    {
                        var (markdownPath, htmlPath) = await outputService.WriteMarkdownAndHtmlAsync(
                            finalContent,
                            blogPost.Title,
                            force);

                        AnsiConsole.MarkupLine($"\n[green]✓[/] Blog post generated successfully!");
                        AnsiConsole.MarkupLine($"[blue]Markdown:[/] {markdownPath}");
                        AnsiConsole.MarkupLine($"[blue]HTML:[/] {htmlPath}");
                    }
                    else
                    {
                        var filePath = await outputService.WriteMarkdownAsync(
                            finalContent,
                            blogPost.Title,
                            force);

                        AnsiConsole.MarkupLine($"\n[green]✓[/] Blog post generated successfully!");
                        AnsiConsole.MarkupLine($"[blue]File:[/] {filePath}");
                    }

                    AnsiConsole.MarkupLine($"[dim]Model used: {selectedModel}[/]");
                    AnsiConsole.MarkupLine($"[dim]Reading time: {blogPost.ReadingTime} minutes[/]");
                    AnsiConsole.MarkupLine($"[dim]Tags: {string.Join(", ", blogPost.Tags)}[/]");
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static List<string> ParseUrls(string input)
    {
        var separators = new[] { ',', ' ', '\n', '\r', ';' };
        return input.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Contains("youtube.com") || s.Contains("youtu.be"))
            .ToList();
    }

    private static async Task<List<string>> ExpandPlaylistsAsync(List<string> urls)
    {
        var videoUrls = new List<string>();

        foreach (var url in urls)
        {
            if (url.Contains("playlist?list="))
            {
                // It's a playlist - expand it
                try
                {
                    var playlistVideos = await YouTubeService.GetPlaylistVideoUrlsAsync(url);
                    videoUrls.AddRange(playlistVideos);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Could not expand playlist {url}: {ex.Message}[/]");
                }
            }
            else
            {
                // Regular video URL
                videoUrls.Add(url);
            }
        }

        return videoUrls;
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
