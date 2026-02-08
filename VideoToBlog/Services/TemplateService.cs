using VideoToBlog.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VideoToBlog.Services;

/// <summary>
/// Service for managing blog post templates.
/// </summary>
public class TemplateService
{
    private readonly string _templatesDirectory;

    public TemplateService(string templatesDirectory = "templates")
    {
        _templatesDirectory = templatesDirectory;
    }

    /// <summary>
    /// Applies a template to a blog post and returns the final content.
    /// </summary>
    public string ApplyTemplate(BlogPost blogPost, string templateName = "default")
    {
        var templatePath = Path.Combine(_templatesDirectory, $"{templateName}.md");
        
        if (!File.Exists(templatePath))
        {
            return GenerateDefaultTemplate(blogPost);
        }

        var template = File.ReadAllText(templatePath);
        return PopulateTemplate(template, blogPost);
    }

    private static string PopulateTemplate(string template, BlogPost blogPost)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var frontmatter = new Dictionary<string, object>
        {
            ["title"] = blogPost.Title,
            ["description"] = blogPost.Description,
            ["date"] = blogPost.Date.ToString("yyyy-MM-dd"),
            ["tags"] = blogPost.Tags,
            ["reading_time"] = $"{blogPost.ReadingTime} min read",
            ["video_url"] = blogPost.VideoUrl
        };

        var yaml = serializer.Serialize(frontmatter).Trim();

        return template
            .Replace("{{TITLE}}", blogPost.Title)
            .Replace("{{DESCRIPTION}}", blogPost.Description)
            .Replace("{{DATE}}", blogPost.Date.ToString("yyyy-MM-dd"))
            .Replace("{{TAGS}}", string.Join(", ", blogPost.Tags))
            .Replace("{{READING_TIME}}", $"{blogPost.ReadingTime} min read")
            .Replace("{{VIDEO_URL}}", blogPost.VideoUrl)
            .Replace("{{CONTENT}}", blogPost.Content)
            .Replace("---\ntitle:", $"---\n{yaml}\n---")
            .TrimStart();
    }

    private static string GenerateDefaultTemplate(BlogPost blogPost)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var frontmatter = new Dictionary<string, object>
        {
            ["title"] = blogPost.Title,
            ["description"] = blogPost.Description,
            ["date"] = blogPost.Date.ToString("yyyy-MM-dd"),
            ["tags"] = blogPost.Tags,
            ["reading_time"] = $"{blogPost.ReadingTime} min read",
            ["video_url"] = blogPost.VideoUrl
        };

        var yaml = serializer.Serialize(frontmatter).Trim();

        return $"""
        ---
        {yaml}
        ---

        # {blogPost.Title}

        {blogPost.Content}

        ---

        *This blog post was generated from a YouTube video transcript.*
        """;
    }
}
