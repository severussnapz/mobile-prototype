namespace Genesis.AI.Domain.Exceptions;

public sealed class GitHubScaffoldFailedException : Exception
{
    public GitHubScaffoldFailedException(string userMessage)
        : base(userMessage)
    {
        UserMessage = userMessage;
    }

    public string UserMessage { get; }
}