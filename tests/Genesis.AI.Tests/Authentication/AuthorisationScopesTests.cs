using Genesis.AI.Api.Authentication;

namespace Genesis.AI.Tests.Authentication;

public class AuthorisationScopesTests
{
    [Theory]
    [InlineData(AuthorisationScopes.Admin, "genai-req.admin")]
    [InlineData(AuthorisationScopes.Read, "genai-req.read")]
    [InlineData(AuthorisationScopes.Write, "genai-req.write")]
    [InlineData(AuthorisationScopes.Architecture, "genai-req.arch")]
    [InlineData(AuthorisationScopes.ProductDesign, "genai-req.pxd")]
    [InlineData(AuthorisationScopes.ClinicalSafety, "genai-req.clin")]
    public void AllScopesMatchExpectedFormat(string scope, string expected)
    {
        Assert.Equal(expected, scope);
    }

    [Theory]
    [InlineData(AuthorisationScopes.Admin)]
    [InlineData(AuthorisationScopes.Read)]
    [InlineData(AuthorisationScopes.Write)]
    [InlineData(AuthorisationScopes.Architecture)]
    [InlineData(AuthorisationScopes.ProductDesign)]
    [InlineData(AuthorisationScopes.ClinicalSafety)]
    public void AllScopesMatchRegexFormat(string scope)
    {
        // AUTH-002: ^([a-z]{3,12})-([a-z.]{3,12})$
        Assert.Matches(@"^([a-z]{3,12})-([a-z.]{3,12})$", scope);
    }
}
