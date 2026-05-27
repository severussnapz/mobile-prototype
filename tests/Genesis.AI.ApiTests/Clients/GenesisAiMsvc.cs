using ApiAutomationCore;

namespace Genesis.AI.ApiTests.Clients;

[MicroserviceLocator]
public class GenesisAiMsvc : MsvcBase<IGenesisAiApi>
{
    public GenesisAiMsvc(string environment, Action<HttpRequestMessage>? modifyHeaders = null)
        : base("genesis-ai", GetBaseUrl(environment), modifyHeaders)
    {
    }

    private static string GetBaseUrl(string environment)
    {
        var explicitUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrEmpty(explicitUrl))
        {
            return explicitUrl.TrimEnd('/');
        }

        if (string.Equals(environment, "local", StringComparison.OrdinalIgnoreCase))
        {
            return "http://localhost:5000";
        }

        if (string.Equals(environment, "ci", StringComparison.OrdinalIgnoreCase))
        {
            return "http://localhost:8080";
        }

        return environment.ToLowerInvariant() switch
        {
            "dev" or "development" => "https://api.platform.dev.emishealthsolutions.com/dev/genesis-ai",
            "int" or "integration" => "https://api.platform.int.emishealthsolutions.com/int/genesis-ai",
            "stg" or "staging" => "https://api.platform.stg.emishealthsolutions.com/stg/genesis-ai",
            "prd" or "production" => "https://api.platform.emishealthsolutions.com/prd/genesis-ai",
            _ => $"https://api.platform.{environment}.emishealthsolutions.com/{environment}/genesis-ai"
        };
    }
}
