namespace Genesis.AI.Api.Features.Conversations;

internal sealed class ToolExecutionFailedException : Exception
{
    public string ToolName { get; }

    public ToolExecutionFailedException(string toolName, string message)
        : base(message)
    {
        ToolName = toolName;
    }

    public ToolExecutionFailedException(string toolName, string message, Exception innerException)
        : base(message, innerException)
    {
        ToolName = toolName;
    }
}
