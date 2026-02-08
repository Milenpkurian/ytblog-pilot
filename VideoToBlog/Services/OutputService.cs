using System.Text.RegularExpressions;
using Markdig;
using VideoToBlog.Models;

namespace VideoToBlog.Services;

/// <summary>
/// Service for managing blog post output files.
/// </summary>
public partial class OutputService
{
    private readonly string _outputDirectory;

    [GeneratedRegex(@"[^\w\s-]", RegexOptions.None)]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"\s+", RegexOptions.None)]
    private static partial Regex WhitespaceRegex();

    public OutputService(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
    }

    /// <summary>
    /// Writes a blog post to a markdown file.
    /// </summary>
    public async Task<string> WriteMarkdownAsync(string content, string title, bool force = false, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_outputDirectory);

        var fileName = GenerateFileName(title);
        var filePath = Path.Combine(_outputDirectory, fileName);

        if (File.Exists(filePath) && !force)
        {
            filePath = GetUniqueFilePath(filePath);
        }

        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        return filePath;
    }

    /// <summary>
    /// Writes a blog post as both markdown and HTML files.
    /// </summary>
    public async Task<(string markdownPath, string htmlPath)> WriteMarkdownAndHtmlAsync(
        string content, 
        string title, 
        bool force = false, 
        CancellationToken cancellationToken = default)
    {
        var markdownPath = await WriteMarkdownAsync(content, title, force, cancellationToken);
        
        var html = ConvertMarkdownToHtml(content);
        var htmlFileName = Path.GetFileNameWithoutExtension(markdownPath) + ".html";
        var htmlPath = Path.Combine(_outputDirectory, htmlFileName);

        await File.WriteAllTextAsync(htmlPath, html, cancellationToken);

        return (markdownPath, htmlPath);
    }

    private static string ConvertMarkdownToHtml(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var htmlBody = Markdown.ToHtml(markdown, pipeline);

        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Blog Post</title>
            <style>
                body {
                    max-width: 800px;
                    margin: 0 auto;
                    padding: 20px;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                    line-height: 1.6;
                    color: #333;
                }
                code {
                    background-color: #f4f4f4;
                    padding: 2px 6px;
                    border-radius: 3px;
                    font-family: 'Courier New', monospace;
                }
                pre {
                    background-color: #f4f4f4;
                    padding: 15px;
                    border-radius: 5px;
                    overflow-x: auto;
                }
                pre code {
                    padding: 0;
                    background-color: transparent;
                }
                h1, h2, h3, h4, h5, h6 {
                    margin-top: 1.5em;
                    margin-bottom: 0.5em;
                }
                a {
                    color: #0066cc;
                }
            </style>
        </head>
        <body>
        {{htmlBody}}
        </body>
        </html>
        """;
    }

    private static string GenerateFileName(string title)
    {
        var slug = title.ToLowerInvariant();
        
        slug = InvalidCharsRegex().Replace(slug, string.Empty);
        slug = WhitespaceRegex().Replace(slug, "-");
        slug = slug.Trim('-');

        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return $"{date}-{slug}.md";
    }

    private static string GetUniqueFilePath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        
        var counter = 1;
        string uniquePath;

        do
        {
            uniquePath = Path.Combine(directory, $"{fileNameWithoutExtension}-{counter}{extension}");
            counter++;
        }
        while (File.Exists(uniquePath));

        return uniquePath;
    }
}
