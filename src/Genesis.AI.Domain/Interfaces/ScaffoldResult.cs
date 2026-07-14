namespace Genesis.AI.Domain.Interfaces;

public sealed record ScaffoldResult(bool IsSuccess, string? FailureReason)
{
    public static ScaffoldResult Success()
    {
        return new ScaffoldResult(true, null);
    }

    public static ScaffoldResult Failure(string failureReason)
    {
        return new ScaffoldResult(false, failureReason);
    }
}