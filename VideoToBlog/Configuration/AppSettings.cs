namespace VideoToBlog.Configuration;

/// <summary>
/// Application configuration settings.
/// </summary>
public class AppSettings
{
    public string CacheDirectory { get; set; } = ".cache";
    public int CacheTtlDays { get; set; } = 7;
    public int MaxTranscriptLength { get; set; } = 10000;
    public int ChunkSize { get; set; } = 1500;
    public string DefaultOutputDirectory { get; set; } = "./output";
    public string DefaultTemplate { get; set; } = "default";
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public int CopilotTimeout { get; set; } = 300;
    public string? SelectedModel { get; set; }
}
