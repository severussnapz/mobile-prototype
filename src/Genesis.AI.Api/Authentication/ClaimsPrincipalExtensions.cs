using System.Security.Claims;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static bool HasScope(this ClaimsPrincipal principal, string scope)
    {
        return principal.FindAll("authorizations")
            .Any(claim => claim.Value == scope);
    }

    public static bool HasAnyScope(this ClaimsPrincipal principal, params string[] scopes)
    {
        var authorisations = principal.FindAll("authorizations").Select(claim => claim.Value).ToHashSet();
        return scopes.Any(authorisations.Contains);
    }

    public static bool CanConverseOnStage(this ClaimsPrincipal principal, StageType stageType)
    {
        if (principal.HasScope(AuthorisationScopes.Admin))
            return true;

        return stageType switch
        {
            StageType.Architecture => principal.HasScope(AuthorisationScopes.Architecture),
            StageType.Pxd => principal.HasScope(AuthorisationScopes.ProductDesign),
            StageType.ClinicalSafety => principal.HasScope(AuthorisationScopes.ClinicalSafety),
            _ => principal.HasScope(AuthorisationScopes.Write)
        };
    }

    public static string? GetUserErn(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("userERN");
    }

    public static string? GetGivenName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("givenName");
    }

    public static string? GetFamilyName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("familyName");
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("email");
    }
}
