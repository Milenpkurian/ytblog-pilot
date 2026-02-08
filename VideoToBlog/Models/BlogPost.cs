namespace VideoToBlog.Models;

/// <summary>
/// Represents a generated blog post.
/// </summary>
public record BlogPost
{
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string Description { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public int ReadingTime { get; init; }
    public required string VideoUrl { get; init; }
    public DateTime Date { get; init; } = DateTime.UtcNow;
}
