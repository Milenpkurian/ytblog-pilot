using FluentAssertions;
using VideoToBlog.Configuration;
using VideoToBlog.Exceptions;
using VideoToBlog.Services;
using Xunit;

namespace VideoToBlog.Tests;

public class YouTubeServiceTests
{
    private readonly AppSettings _settings = new()
    {
        CacheDirectory = ".cache-test",
        CacheTtlDays = 7,
        MaxTranscriptLength = 10000,
        MaxRetries = 3,
        RetryDelaySeconds = 1
    };

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=abc123DEF-_")]
    public async Task GetVideoInfoAsync_WithValidUrls_DoesNotThrowException(string url)
    {
        var service = new YouTubeService(_settings);
        
        var action = async () => await service.GetVideoInfoAsync(url);
        
        await action.Should().NotThrowAsync<YouTubeUrlException>();
    }

    [Theory]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("not a url")]
    [InlineData("https://youtube.com")]
    public async Task GetVideoInfoAsync_WithInvalidUrls_ThrowsYouTubeUrlException(string url)
    {
        var service = new YouTubeService(_settings);

        var action = async () => await service.GetVideoInfoAsync(url);

        await action.Should().ThrowAsync<YouTubeUrlException>();
    }
}
