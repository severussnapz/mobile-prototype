namespace Genesis.AI.Domain.Exceptions;

public sealed class GitHubAuthenticationException : Exception
{
    public GitHubAuthenticationException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}