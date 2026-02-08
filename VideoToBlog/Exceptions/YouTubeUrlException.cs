namespace VideoToBlog.Exceptions;

/// <summary>
/// Exception thrown when a YouTube URL is invalid.
/// </summary>
public class YouTubeUrlException : Exception
{
    public YouTubeUrlException(string message) : base(message)
    {
    }

    public YouTubeUrlException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
