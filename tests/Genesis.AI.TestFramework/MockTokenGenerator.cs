using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Genesis.AI.TestFramework;

public sealed class MockTokenGenerator
{
    public const string DefaultIssuer = "https://identity.test.emishealthsolutions.com/test-tenant/v2.0/";
    public const string DefaultAudience = "genesis-ai-requirements-api";

    public string Issuer { get; } = DefaultIssuer;
    public string Audience { get; } = DefaultAudience;

    public const string TestUserErn = "ern:emis:user:user:7ae7b859-c480-4644-b3be-d77d628f6e7e";
    public const string TestOrgErn = "ern:emis:org:org:5f1ffc31-aa85-463a-8683-1db544d95fec";
    public const string TestOrgName = "EMIS Admin";

    public SecurityKey SigningKey { get; }

    public MockTokenGenerator()
    {
        var keyBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        SigningKey = new SymmetricSecurityKey(keyBytes);
    }

    public string CreateBearerToken(params string[] authorisations)
    {
        return CreateBearerToken(TestUserErn, "Test User", authorisations);
    }

    public string CreateBearerToken(string userErn, string userName, params string[] authorisations)
    {
        var credentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("userERN", userErn),
            new("orgERN", TestOrgErn),
            new("orgName", TestOrgName),
            new("email", "test.user@emishealth.com"),
            new("givenName", userName.Split(' ').FirstOrDefault() ?? "Test"),
            new("familyName", userName.Split(' ').LastOrDefault() ?? "User"),
            new("scp", "emis-x"),
        };

        foreach (var auth in authorisations)
        {
            claims.Add(new Claim("authorizations", auth));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateAdminToken()
    {
        return CreateBearerToken(
            "genai-req.admin",
            "genai-req.read",
            "genai-req.write",
            "genai-req.arch",
            "genai-req.pxd",
            "genai-req.clin");
    }

    public string CreateReadOnlyToken()
    {
        return CreateBearerToken("genai-req.read");
    }

    public string CreateWriteToken()
    {
        return CreateBearerToken("genai-req.read", "genai-req.write");
    }

    public string CreateExpiredToken(params string[] authorisations)
    {
        var credentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("userERN", TestUserErn),
            new("orgERN", TestOrgErn),
            new("orgName", TestOrgName),
            new("scp", "emis-x"),
        };

        foreach (var auth in authorisations)
        {
            claims.Add(new Claim("authorizations", auth));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1),
            notBefore: DateTime.UtcNow.AddHours(-2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
