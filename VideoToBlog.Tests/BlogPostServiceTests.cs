using FluentAssertions;
using Moq;
using VideoToBlog.Configuration;
using VideoToBlog.Models;
using VideoToBlog.Services;
using Xunit;

namespace VideoToBlog.Tests;

public class BlogPostServiceTests
{
    private readonly AppSettings _settings = new()
    {
        ChunkSize = 1500
    };

    [Fact]
    public async Task GenerateBlogPostAsync_WithValidVideoInfo_ReturnsBlogPostWithCorrectTitle()
    {
        var service = new BlogPostService(_settings);
        var videoInfo = new VideoInfo
        {
            Title = "Test Video",
            Url = "https://youtube.com/watch?v=test",
            Transcript = "This is a test transcript with some content to process."
        };

        var result = await service.GenerateBlogPostAsync(videoInfo);

        result.Title.Should().Be("Test Video");
        result.VideoUrl.Should().Be("https://youtube.com/watch?v=test");
    }

    [Fact]
    public async Task GenerateBlogPostAsync_CalculatesReadingTimeCorrectly()
    {
        var service = new BlogPostService(_settings);
        var transcript = string.Join(" ", Enumerable.Repeat("word", 200));
        var videoInfo = new VideoInfo
        {
            Title = "Test",
            Url = "https://youtube.com/watch?v=test",
            Transcript = transcript
        };

        var result = await service.GenerateBlogPostAsync(videoInfo);

        result.ReadingTime.Should().BeGreaterThan(0);
    }
}
