using System.Security.Claims;
using Genesis.AI.Api.Authentication;

namespace Genesis.AI.Tests.Authentication;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void HasScope_WhenScopePresent_ReturnsTrue()
    {
        var principal = CreatePrincipalWithScopes("genai-req.write", "genai-req.read");

        Assert.True(principal.HasScope("genai-req.write"));
    }

    [Fact]
    public void HasScope_WhenScopeAbsent_ReturnsFalse()
    {
        var principal = CreatePrincipalWithScopes("genai-req.read");

        Assert.False(principal.HasScope("genai-req.write"));
    }

    [Fact]
    public void HasScope_WhenNoClaims_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(principal.HasScope("genai-req.admin"));
    }

    [Fact]
    public void HasAnyScope_WhenOneMatches_ReturnsTrue()
    {
        var principal = CreatePrincipalWithScopes("genai-req.arch");

        Assert.True(principal.HasAnyScope("genai-req.admin", "genai-req.arch"));
    }

    [Fact]
    public void HasAnyScope_WhenNoneMatch_ReturnsFalse()
    {
        var principal = CreatePrincipalWithScopes("genai-req.read");

        Assert.False(principal.HasAnyScope("genai-req.admin", "genai-req.write"));
    }

    [Fact]
    public void GetUserErn_WhenClaimPresent_ReturnsValue()
    {
        var claims = new[]
        {
            new Claim("userERN", "ern:emis:user:user:f2612ca0-4be2-4eac-bb5c-381db1f9eec2")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        Assert.Equal("ern:emis:user:user:f2612ca0-4be2-4eac-bb5c-381db1f9eec2", principal.GetUserErn());
    }

    [Fact]
    public void GetUserErn_WhenApplicationRestrictedToken_ReturnsNull()
    {
        var claims = new[]
        {
            new Claim("appERN", "ern:emis:user:app:c211b4c0-e967-4bda-a875-fa05959deae0")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        Assert.Null(principal.GetUserErn());
    }

    [Fact]
    public void GetEmail_WhenClaimPresent_ReturnsValue()
    {
        var claims = new[]
        {
            new Claim("email", "luke.smith@emishealth.com")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        Assert.Equal("luke.smith@emishealth.com", principal.GetEmail());
    }

    private static ClaimsPrincipal CreatePrincipalWithScopes(params string[] scopes)
    {
        var claims = scopes.Select(s => new Claim("authorizations", s)).ToList();
        var identity = new ClaimsIdentity(claims, "Bearer");
        return new ClaimsPrincipal(identity);
    }
}
