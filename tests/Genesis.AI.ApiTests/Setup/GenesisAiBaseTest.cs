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

    private static readonly Random CodeRandom = new();

    /// <summary>
    /// Generates a project code that satisfies the API validation rule (^[A-Z]+$, 3-10 characters).
    /// The supplied prefix is sanitised to uppercase letters only and padded with random uppercase letters.
    /// </summary>
    protected static string GenerateProjectCode(string prefix)
    {
        var sanitised = new string(prefix.Where(char.IsAsciiLetterUpper).ToArray());
        var builder = new System.Text.StringBuilder(sanitised);
        while (builder.Length < 10)
        {
            lock (CodeRandom)
            {
                builder.Append((char)('A' + CodeRandom.Next(0, 26)));
            }
        }

        return builder.ToString()[..10];
    }

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
