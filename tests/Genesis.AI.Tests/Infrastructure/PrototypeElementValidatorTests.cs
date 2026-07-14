using System.Reflection;

namespace Genesis.AI.Tests.Infrastructure;

public sealed class PrototypeElementValidatorTests
{
    [Fact]
    public void ExtractMarkerReason_WhenReasonEndsWithDash_PreservesDash()
    {
        const string modelOutput = "<!-- OUT_OF_SCOPE: trailing dash- -->";

        var reason = InvokeExtractMarkerReason(modelOutput, "OUT_OF_SCOPE");

        Assert.Equal("trailing dash-", reason);
    }

    [Fact]
    public void ExtractMarkerReason_WhenReasonEndsWithAngle_PreservesAngle()
    {
        const string modelOutput = "<!-- OUT_OF_SCOPE: ends with > -->";

        var reason = InvokeExtractMarkerReason(modelOutput, "OUT_OF_SCOPE");

        Assert.Equal("ends with >", reason);
    }

    [Fact]
    public void ExtractMarkerReason_WhenReasonHasHtmlComment_StripsComment()
    {
        const string modelOutput = "<!-- OUT_OF_SCOPE: real reason -->";

        var reason = InvokeExtractMarkerReason(modelOutput, "OUT_OF_SCOPE");

        Assert.Equal("real reason", reason);
    }

    private static string InvokeExtractMarkerReason(string modelOutput, string marker)
    {
        var validatorType = Type.GetType(
            "Genesis.AI.Infrastructure.Services.PrototypeElementValidator, Genesis.AI.Infrastructure",
            throwOnError: false);

        Assert.NotNull(validatorType);

        var method = validatorType!.GetMethod(
            "ExtractMarkerReason",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var result = method!.Invoke(null, [modelOutput, marker]);

        Assert.NotNull(result);
        return (string)result!;
    }
}
