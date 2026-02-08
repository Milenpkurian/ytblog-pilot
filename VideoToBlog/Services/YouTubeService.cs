using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Polly;
using Polly.Retry;
using VideoToBlog.Configuration;
using VideoToBlog.Exceptions;
using VideoToBlog.Models;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

namespace VideoToBlog.Services;

/// <summary>
/// Service for extracting video transcripts from YouTube.
/// </summary>
public partial class YouTubeService
{
    private readonly YoutubeClient _youtubeClient;
    private readonly AppSettings _settings;
    private readonly ResiliencePipeline _retryPipeline;
    
    [GeneratedRegex(@"(?:youtube\.com/watch\?v=|youtu\.be/)([\w-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex YouTubeUrlRegex();

    [GeneratedRegex(@"(?:youtube\.com/playlist\?list=)([\w-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PlaylistUrlRegex();

    public YouTubeService(AppSettings settings)
    {
        _settings = settings;
        _youtubeClient = new YoutubeClient();
        
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _settings.MaxRetries,
                Delay = TimeSpan.FromSeconds(_settings.RetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential
            })
            .Build();
    }

    /// <summary>
    /// Extracts video information including metadata and transcript from a YouTube URL.
    /// </summary>
    public async Task<VideoInfo> GetVideoInfoAsync(string youtubeUrl, CancellationToken cancellationToken = default)
    {
        if (!IsValidYouTubeUrl(youtubeUrl))
        {
            throw new YouTubeUrlException($"Invalid YouTube URL: {youtubeUrl}");
        }

        var videoId = ExtractVideoId(youtubeUrl);
        var cacheKey = GetCacheKey(youtubeUrl);
        
        var cachedInfo = await GetCachedVideoInfoAsync(cacheKey, cancellationToken);
        if (cachedInfo is not null)
        {
            return cachedInfo;
        }

        var videoInfo = await _retryPipeline.ExecuteAsync(async token =>
        {
            var video = await _youtubeClient.Videos.GetAsync(videoId, token);
            var transcript = await GetTranscriptAsync(videoId, token);

            var info = new VideoInfo
            {
                Title = video.Title,
                Url = youtubeUrl,
                Transcript = transcript,
                Duration = video.Duration,
                ThumbnailUrl = video.Thumbnails.FirstOrDefault()?.Url,
                PublishDate = video.UploadDate.DateTime,
                Author = video.Author.ChannelTitle
            };

            await CacheVideoInfoAsync(cacheKey, info, token);
            return info;
        }, cancellationToken);

        return videoInfo;
    }

    private async Task<string> GetTranscriptAsync(string videoId, CancellationToken cancellationToken)
    {
        try
        {
            var trackManifest = await _youtubeClient.Videos.ClosedCaptions.GetManifestAsync(videoId, cancellationToken);
            
            var trackInfo = trackManifest.Tracks
                .FirstOrDefault(t => t.Language.Code.StartsWith("en")) 
                ?? trackManifest.Tracks.FirstOrDefault();

            if (trackInfo is null)
            {
                throw new YouTubeUrlException("No transcript available for this video. The video may not have captions enabled.");
            }

            var track = await _youtubeClient.Videos.ClosedCaptions.GetAsync(trackInfo, cancellationToken);
            
            var transcript = new StringBuilder();
            foreach (var caption in track.Captions)
            {
                transcript.AppendLine(caption.Text);
            }

            var sanitized = SanitizeTranscript(transcript.ToString());
            
            if (sanitized.Split(' ').Length > _settings.MaxTranscriptLength)
            {
                throw new YouTubeUrlException($"Transcript exceeds maximum length of {_settings.MaxTranscriptLength} words");
            }

            return sanitized;
        }
        catch (YoutubeExplode.Exceptions.VideoUnavailableException ex)
        {
            throw new YouTubeUrlException($"Video is not available. It may be private, deleted, or region-restricted. Video ID: {videoId}", ex);
        }
    }

    private static string SanitizeTranscript(string transcript)
    {
        transcript = Regex.Replace(transcript, @"\[[\d:]+\]", string.Empty);
        transcript = Regex.Replace(transcript, @"\s+", " ");
        transcript = transcript.Trim();
        
        return transcript;
    }

    private static bool IsValidYouTubeUrl(string url)
    {
        return YouTubeUrlRegex().IsMatch(url);
    }

    private static string ExtractVideoId(string url)
    {
        var match = YouTubeUrlRegex().Match(url);
        return match.Success ? match.Groups[1].Value : throw new YouTubeUrlException("Could not extract video ID from URL");
    }

    private static string GetCacheKey(string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(hash);
    }

    private async Task<VideoInfo?> GetCachedVideoInfoAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var cacheFilePath = Path.Combine(_settings.CacheDirectory, $"{cacheKey}.json");
        
        if (!File.Exists(cacheFilePath))
        {
            return null;
        }

        var fileInfo = new FileInfo(cacheFilePath);
        if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc > TimeSpan.FromDays(_settings.CacheTtlDays))
        {
            File.Delete(cacheFilePath);
            return null;
        }

        var json = await File.ReadAllTextAsync(cacheFilePath, cancellationToken);
        return System.Text.Json.JsonSerializer.Deserialize<VideoInfo>(json);
    }

    private async Task CacheVideoInfoAsync(string cacheKey, VideoInfo videoInfo, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_settings.CacheDirectory);
        
        var cacheFilePath = Path.Combine(_settings.CacheDirectory, $"{cacheKey}.json");
        var json = System.Text.Json.JsonSerializer.Serialize(videoInfo, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        await File.WriteAllTextAsync(cacheFilePath, json, cancellationToken);
    }

    /// <summary>
    /// Gets all video URLs from a YouTube playlist.
    /// </summary>
    public static async Task<List<string>> GetPlaylistVideoUrlsAsync(string playlistUrl, CancellationToken cancellationToken = default)
    {
        if (!IsValidPlaylistUrl(playlistUrl))
        {
            throw new YouTubeUrlException($"Invalid YouTube playlist URL: {playlistUrl}");
        }

        var youtubeClient = new YoutubeClient();
        var videoUrls = new List<string>();

        await foreach (var video in youtubeClient.Playlists.GetVideosAsync(playlistUrl, cancellationToken))
        {
            videoUrls.Add($"https://www.youtube.com/watch?v={video.Id}");
        }

        return videoUrls;
    }

    private static bool IsValidPlaylistUrl(string url)
    {
        return PlaylistUrlRegex().IsMatch(url);
    }
}
