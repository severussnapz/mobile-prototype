using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Genesis.AI.Api.Authentication;
using Genesis.AI.TestFramework;
using Microsoft.IdentityModel.Tokens;

namespace Genesis.AI.Tests.Authentication;

/// <summary>
/// Regression tests: validates a write token end-to-end (JWT generation → validation →
/// ClaimsPrincipal) and asserts the resulting claims satisfy the ProjectWrite policy.
/// Pins the fix for the CreateWriteToken() overload-resolution defect (Day 3b fix):
/// CreateBearerToken("genai-req.read", "genai-req.write") previously resolved to the
/// CreateBearerToken(string userErn, string userName, params string[]) overload, producing
/// a token with no authorizations claims at all.
/// </summary>
public class MockTokenWriteScopeTests
{
    private static ClaimsPrincipal ValidateToken(string jwt, MockTokenGenerator generator)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.MapInboundClaims = false;   // mirrors JwtBearerOptions in Program.cs + PostConfigure

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = generator.Issuer,
            ValidateAudience = true,
            ValidAudience = generator.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = generator.SigningKey,
            ValidateLifetime = false        // don't care about expiry in unit test
        };

        var principal = handler.ValidateToken(jwt, parameters, out _);
        return principal;
    }

    [Fact]
    public void WriteToken_WhenValidated_ContainsWriteAuthorizationClaim()
    {
        var generator = new MockTokenGenerator();
        var jwt = generator.CreateWriteToken();

        var principal = ValidateToken(jwt, generator);

        // Dump all claim types + values in the failure message so we can see
        // exactly what the JWT round-trip produces.
        var allClaims = string.Join(", ", principal.Claims.Select(c => $"[{c.Type}={c.Value}]"));

        Assert.True(
            principal.Claims.Any(c => c.Type == "authorizations" && c.Value == "genai-req.write"),
            $"Expected claim authorizations=genai-req.write but found: {allClaims}");
    }

    [Fact]
    public void WriteToken_WhenValidated_SatisfiesProjectWritePolicy()
    {
        // Mirrors RequireClaim("authorizations", "genai-req.write", "genai-req.admin")
        var generator = new MockTokenGenerator();
        var jwt = generator.CreateWriteToken();

        var principal = ValidateToken(jwt, generator);

        var allClaims = string.Join(", ", principal.Claims.Select(c => $"[{c.Type}={c.Value}]"));
        var allowedScopes = new[] { AuthorisationScopes.Write, AuthorisationScopes.Admin };

        Assert.True(
            principal.Claims.Any(c =>
                string.Equals(c.Type, "authorizations", StringComparison.OrdinalIgnoreCase) &&
                allowedScopes.Contains(c.Value)),
            $"Expected ProjectWrite-satisfying claim but found: {allClaims}");
    }
}
