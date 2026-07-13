namespace Genesis.AI.Domain.Exceptions;

public sealed class GitHubFileTooLargeException : Exception
{
    public GitHubFileTooLargeException(string message)
        : base(message)
    {
    }
}