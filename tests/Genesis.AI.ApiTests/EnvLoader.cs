using System.Runtime.CompilerServices;

namespace Genesis.AI.ApiTests;

internal static class EnvLoader
{
    [ModuleInitializer]
    internal static void LoadEnv()
    {
        var dir = Directory.GetCurrentDirectory();
        var envFile = FindEnvFile(dir);

        if (envFile is null)
            return;

        var values = ParseEnvFile(envFile);

        // Set fixed test environment values (don't override if already set via -e flags in CI)
        SetIfNotSet("Environment", "local");
        SetIfNotSet("UsersEnvironment", "dev");
        SetIfNotSet("API_BASE_URL", "http://localhost:5000");

        // Map .env USERNAME/PASSWORD to the config keys the framework expects
        if (values.TryGetValue("USERNAME", out var username))
            SetIfNotSet("ApiConfiguration__Username", username);

        if (values.TryGetValue("PASSWORD", out var password))
            SetIfNotSet("ApiConfiguration__Password", password);
    }

    private static void SetIfNotSet(string key, string value)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, value);
    }

    private static string? FindEnvFile(string startDir)
    {
        var dir = startDir;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, ".env");
            if (File.Exists(candidate))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }

    private static Dictionary<string, string> ParseEnvFile(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0)
                continue;

            var key = trimmed[..eqIndex].Trim();
            var value = trimmed[(eqIndex + 1)..].Trim().Trim('"');
            result[key] = value;
        }

        return result;
    }
}
