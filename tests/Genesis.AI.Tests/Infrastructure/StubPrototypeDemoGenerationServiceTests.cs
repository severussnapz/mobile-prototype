using Genesis.AI.Infrastructure.Services;
using Genesis.AI.Tests.PrototypeDemo;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

// Day 0 harness (service level): pins the four content properties of the
// streaming prototype-demo output. GenerateAsync returns IAsyncEnumerable<string>
// so the Day 2 SSE switch is a controller change, not a service rewrite; here we
// collect the stream synchronously. Fails to compile until the Day 1 contract
// exists (IPrototypeDemoGenerationService + StubPrototypeDemoGenerationService).
public class StubPrototypeDemoGenerationServiceTests
{
    private static async Task<string> GenerateHtmlAsync()
    {
        var service = new StubPrototypeDemoGenerationService();
        return await PrototypeDemoHtmlAssertions.CollectAsync(
            service.GenerateAsync(Guid.Empty, "Demo Project", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_WhenCalled_IncludesPrototypeOnlyBanner()
    {
        PrototypeDemoHtmlAssertions.AssertContainsPrototypeOnlyBanner(await GenerateHtmlAsync());
    }

    [Fact]
    public async Task GenerateAsync_WhenCalled_InlinesEmisXBaseCssIntoHead()
    {
        PrototypeDemoHtmlAssertions.AssertEmisXBaseCssInlinedIntoHead(await GenerateHtmlAsync());
    }

    [Fact]
    public async Task GenerateAsync_WhenCalled_ReturnsCompleteHtmlDocument()
    {
        PrototypeDemoHtmlAssertions.AssertCompleteHtmlDocument(await GenerateHtmlAsync());
    }

    [Fact]
    public async Task GenerateAsync_WhenCalled_ContainsNoFormatValidNhsNumbers()
    {
        PrototypeDemoHtmlAssertions.AssertNoFormatValidNhsNumbers(await GenerateHtmlAsync());
    }
}
