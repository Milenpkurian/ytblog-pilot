namespace VideoToBlog.Exceptions;

/// <summary>
/// Exception thrown when Copilot processing fails.
/// </summary>
public class CopilotException : Exception
{
    public CopilotException(string message) : base(message)
    {
    }

    public CopilotException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
