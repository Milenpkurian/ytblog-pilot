using System.Diagnostics;
using VideoToBlog.Configuration;
using VideoToBlog.Exceptions;
using VideoToBlog.Models;

namespace VideoToBlog.Services;

/// <summary>
/// Service for generating blog posts using GitHub Copilot CLI.
/// </summary>
public class BlogPostService
{
    private readonly AppSettings _settings;

    public BlogPostService(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Generates a blog post from video information using GitHub Copilot CLI.
    /// </summary>
    public async Task<BlogPost> GenerateBlogPostAsync(VideoInfo videoInfo, CancellationToken cancellationToken = default)
    {
        var chunks = ChunkTranscript(videoInfo.Transcript);
        var contentBuilder = new System.Text.StringBuilder();

        foreach (var chunk in chunks)
        {
            var prompt = BuildPrompt(videoInfo.Title, chunk, chunks.Count > 1);
            var generatedContent = await CallGitHubCopilotAsync(prompt, cancellationToken);
            contentBuilder.AppendLine(generatedContent);
            contentBuilder.AppendLine();
        }

        var content = contentBuilder.ToString().Trim();
        var description = ExtractDescription(content);
        var tags = ExtractTags(videoInfo.Title, content);
        var readingTime = CalculateReadingTime(content);

        return new BlogPost
        {
            Title = videoInfo.Title,
            Content = content,
            Description = description,
            Tags = tags,
            ReadingTime = readingTime,
            VideoUrl = videoInfo.Url
        };
    }

    /// <summary>
    /// Generates a comprehensive blog post from multiple videos.
    /// </summary>
    public async Task<BlogPost> GenerateBlogPostFromMultipleVideosAsync(List<VideoInfo> videoInfos, CancellationToken cancellationToken = default)
    {
        if (videoInfos.Count == 0)
        {
            throw new ArgumentException("At least one video is required", nameof(videoInfos));
        }

        if (videoInfos.Count == 1)
        {
            return await GenerateBlogPostAsync(videoInfos[0], cancellationToken);
        }

        // Combine all transcripts with section markers
        var combinedTranscript = new System.Text.StringBuilder();
        combinedTranscript.AppendLine("This blog post is based on the following videos:");
        combinedTranscript.AppendLine();

        for (int i = 0; i < videoInfos.Count; i++)
        {
            var video = videoInfos[i];
            combinedTranscript.AppendLine($"Video {i + 1}: {video.Title}");
            combinedTranscript.AppendLine($"Author: {video.Author}");
            combinedTranscript.AppendLine($"URL: {video.Url}");
            combinedTranscript.AppendLine();
            combinedTranscript.AppendLine("Transcript:");
            combinedTranscript.AppendLine(video.Transcript);
            combinedTranscript.AppendLine();
            combinedTranscript.AppendLine("---");
            combinedTranscript.AppendLine();
        }

        // Generate a comprehensive title
        var titles = videoInfos.Select(v => v.Title).ToList();
        var commonTheme = ExtractCommonTheme(titles);
        var blogTitle = string.IsNullOrEmpty(commonTheme) 
            ? $"Comprehensive Guide: {videoInfos.Count} Essential Videos" 
            : $"Complete Guide to {commonTheme}";

        // Process the combined content
        var chunks = ChunkTranscript(combinedTranscript.ToString());
        var contentBuilder = new System.Text.StringBuilder();

        // Add introduction
        contentBuilder.AppendLine("## Overview");
        contentBuilder.AppendLine();
        contentBuilder.AppendLine($"This comprehensive guide synthesizes insights from {videoInfos.Count} video(s), providing you with a complete understanding of the topic.");
        contentBuilder.AppendLine();

        // Add source videos section
        contentBuilder.AppendLine("## Source Videos");
        contentBuilder.AppendLine();
        for (int i = 0; i < videoInfos.Count; i++)
        {
            var video = videoInfos[i];
            contentBuilder.AppendLine($"{i + 1}. **[{video.Title}]({video.Url})**");
            contentBuilder.AppendLine($"   - Author: {video.Author}");
            if (video.Duration.HasValue)
            {
                contentBuilder.AppendLine($"   - Duration: {video.Duration.Value:hh\\:mm\\:ss}");
            }
            contentBuilder.AppendLine();
        }
        contentBuilder.AppendLine();

        // Generate main content
        foreach (var chunk in chunks)
        {
            var prompt = BuildMultiVideoPrompt(blogTitle, chunk, chunks.Count > 1, videoInfos.Count);
            var generatedContent = await CallGitHubCopilotAsync(prompt, cancellationToken);
            contentBuilder.AppendLine(generatedContent);
            contentBuilder.AppendLine();
        }

        var content = contentBuilder.ToString().Trim();
        var description = ExtractDescription(content);
        
        // Combine tags from all videos
        var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var video in videoInfos)
        {
            var videoTags = ExtractTags(video.Title, video.Transcript);
            foreach (var tag in videoTags)
            {
                allTags.Add(tag);
            }
        }

        var readingTime = CalculateReadingTime(content);

        // Use the first video URL as primary, or create a summary
        var primaryUrl = videoInfos[0].Url;

        return new BlogPost
        {
            Title = blogTitle,
            Content = content,
            Description = description,
            Tags = allTags.Take(10).ToList(),
            ReadingTime = readingTime,
            VideoUrl = primaryUrl
        };
    }

    private static string ExtractCommonTheme(List<string> titles)
    {
        if (titles.Count == 0) return string.Empty;

        // Simple approach: find common significant words
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", 
            "of", "with", "by", "from", "how", "what", "why", "when", "where"
        };

        var wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var title in titles)
        {
            var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var cleanWord = word.Trim(':', ',', '.', '!', '?', '-');
                if (cleanWord.Length > 3 && !stopWords.Contains(cleanWord))
                {
                    wordFrequency[cleanWord] = wordFrequency.GetValueOrDefault(cleanWord) + 1;
                }
            }
        }

        var mostCommon = wordFrequency
            .Where(kvp => kvp.Value > 1)
            .OrderByDescending(kvp => kvp.Value)
            .Take(2)
            .Select(kvp => kvp.Key)
            .ToList();

        return mostCommon.Count > 0 ? string.Join(" ", mostCommon) : string.Empty;
    }

    private static string BuildMultiVideoPrompt(string title, string transcriptChunk, bool isMultiPart, int videoCount)
    {
        var partIndicator = isMultiPart ? " (this is part of a longer transcript)" : string.Empty;
        
        return $"""
        You are a professional blog writer. Convert the following combined transcript from {videoCount} YouTube videos into a cohesive, well-structured, comprehensive blog post section.
        
        Blog Title: {title}
        {partIndicator}
        
        Requirements:
        - Synthesize information from all videos into a unified narrative
        - Use clear, professional tone
        - Format in Markdown with proper headings (##, ###)
        - Include code examples if mentioned in transcripts
        - Break content into digestible sections
        - Add emphasis using **bold** and *italic* where appropriate
        - Reference different videos when relevant (e.g., "As shown in Video 1...")
        - Do NOT include the blog title as an H1 heading (it will be added separately)
        - Create a comprehensive guide that flows naturally
        
        Combined Transcript:
        {transcriptChunk}
        
        Generate only the blog content, no preamble or explanation:
        """;
    }

    private List<string> ChunkTranscript(string transcript)
    {
        var words = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var currentChunk = new List<string>();
        var wordCount = 0;

        foreach (var word in words)
        {
            currentChunk.Add(word);
            wordCount++;

            if (wordCount >= _settings.ChunkSize)
            {
                chunks.Add(string.Join(" ", currentChunk));
                currentChunk.Clear();
                wordCount = 0;
            }
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(string.Join(" ", currentChunk));
        }

        return chunks;
    }

    private static string BuildPrompt(string title, string transcriptChunk, bool isMultiPart)
    {
        var partIndicator = isMultiPart ? " (this is part of a longer transcript)" : string.Empty;
        
        return $"""
        You are a professional blog writer. Convert the following YouTube video transcript into a well-structured, engaging blog post section.
        
        Video Title: {title}
        {partIndicator}
        
        Requirements:
        - Use clear, professional tone
        - Format in Markdown with proper headings (##, ###)
        - Include code examples if mentioned in transcript
        - Break content into digestible sections
        - Add emphasis using **bold** and *italic* where appropriate
        - Do NOT include the video title as an H1 heading (it will be added separately)
        
        Transcript:
        {transcriptChunk}
        
        Generate only the blog content, no preamble or explanation:
        """;
    }

    private async Task<string> CallGitHubCopilotAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var arguments = $"-p \"{EscapeArgument(prompt)}\" --silent --allow-all";
            
            // Add model selection if specified
            if (!string.IsNullOrEmpty(_settings.SelectedModel))
            {
                arguments += $" --model {_settings.SelectedModel}";
            }

            // Log the command for debugging (optional - can be controlled by verbose flag)
            var commandToExecute = $"copilot {arguments}";
            System.Diagnostics.Debug.WriteLine($"Executing: {commandToExecute.Substring(0, Math.Min(100, commandToExecute.Length))}...");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "copilot",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new CopilotException($"GitHub Copilot CLI failed with exit code {process.ExitCode}: {error}");
            }

            return output.Trim();
        }
        catch (Exception ex) when (ex is not CopilotException)
        {
            throw new CopilotException("Failed to call GitHub Copilot CLI. Ensure 'copilot' is installed and authenticated.", ex);
        }
    }

    private static string EscapeArgument(string arg)
    {
        return arg.Replace("\"", "\\\"").Replace("\n", " ");
    }

    private static string ExtractDescription(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var firstParagraph = lines.FirstOrDefault(l => !l.StartsWith('#') && l.Length > 50);
        
        if (firstParagraph is null)
        {
            return "A blog post generated from a YouTube video transcript.";
        }

        return firstParagraph.Length > 160 
            ? firstParagraph[..157] + "..." 
            : firstParagraph;
    }

    private static List<string> ExtractTags(string title, string content)
    {
        var commonTechWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dotnet", ".net", "csharp", "c#", "python", "javascript", "typescript", 
            "java", "react", "angular", "vue", "docker", "kubernetes", "aws", "azure",
            "tutorial", "guide", "howto", "tips", "tricks", "best practices"
        };

        var tags = new List<string>();
        var allText = $"{title} {content}".ToLowerInvariant();

        foreach (var word in commonTechWords)
        {
            if (allText.Contains(word))
            {
                tags.Add(word);
            }
        }

        return tags.Take(5).ToList();
    }

    private static int CalculateReadingTime(string content)
    {
        const int wordsPerMinute = 200;
        var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = (int)Math.Ceiling(wordCount / (double)wordsPerMinute);
        
        return Math.Max(1, minutes);
    }
}
