using System.Net;
using Genesis.AI.ApiTests.Clients;
using Refit;

namespace Genesis.AI.ApiTests.Setup;

[Trait("Category", "API")]
public abstract class GenesisAiBaseTest(GenesisAiFixture fixture) : IClassFixture<GenesisAiFixture>
{
    protected GenesisAiFixture Fixture { get; } = fixture;
    protected GenesisAiMsvc Msvc { get; } = new(fixture.Environment);

    protected string ValidToken => Fixture.ValidToken ?? throw new InvalidOperationException("Valid token not available");

    protected static async Task<string> ReadContentAsync<T>(ApiResponse<T> response)
    {
        if (response.Error != null)
        {
            return response.Error.Content ?? string.Empty;
        }

        if (response.Content is HttpResponseMessage httpResponse)
        {
            return await httpResponse.Content.ReadAsStringAsync();
        }

        return response.Content?.ToString() ?? string.Empty;
    }

    protected static void AssertNotFound<T>(ApiResponse<T> response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
