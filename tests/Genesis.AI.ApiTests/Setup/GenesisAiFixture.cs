using System.Text.Json;
using ApiAutomationCore.TestSetup;

namespace Genesis.AI.ApiTests.Setup;

public class GenesisAiFixture : AuthenticatedFixture, IAsyncLifetime
{
    private static readonly HttpClient TokenClient = new();

    public string? UnauthorizedToken { get; private set; }

    private readonly string _username;
    private readonly string _password;
    private readonly string _clientId;
    private readonly string _tenantId;
    private readonly string _scope;
    private readonly string _orgErn;

    public GenesisAiFixture()
    {
        _username = ApiConfiguration.Username ?? string.Empty;
        _password = ApiConfiguration.Password ?? string.Empty;
        _clientId = ApiConfiguration.ClientId ?? string.Empty;
        _tenantId = ApiConfiguration.TenantId ?? string.Empty;
        _scope = ApiConfiguration.Scope ?? string.Empty;
        _orgErn = ApiConfiguration.OrgErn ?? string.Empty;
    }

    public new async ValueTask InitializeAsync()
    {
        ValidToken = await GetTokenViaRopcAsync(_scope);
        UnauthorizedToken = await GetTokenViaRopcAsync("openid");
    }

    public new ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public async Task<string?> GetTokenWithScopesAsync(params string[] scopes)
    {
        var combinedScope = $"openid {string.Join(' ', scopes)}";
        return await GetTokenViaRopcAsync(combinedScope);
    }

    private async Task<string?> GetTokenViaRopcAsync(string scope)
    {
        if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
        {
            return null;
        }

        var encodedOrgErn = Uri.EscapeDataString(_orgErn);
        var tokenEndpoint = $"https://identity.dev.emishealthsolutions.com/{_tenantId}/B2C_1A_ROPCV2/oauth2/v2.0/token?orgERN={encodedOrgErn}";

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _clientId,
            ["username"] = _username,
            ["password"] = _password,
            ["scope"] = scope,
            ["response_type"] = "token"
        });

        var response = await TokenClient.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString();
    }
}
