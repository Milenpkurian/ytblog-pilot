namespace VideoToBlog.Models;

/// <summary>
/// Represents information about a YouTube video.
/// </summary>
public record VideoInfo
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string Transcript { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? ThumbnailUrl { get; init; }
    public DateTime? PublishDate { get; init; }
    public string? Author { get; init; }
}
