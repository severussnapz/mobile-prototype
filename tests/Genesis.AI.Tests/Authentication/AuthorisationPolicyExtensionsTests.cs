using Genesis.AI.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Genesis.AI.Tests.Authentication;

public class AuthorisationPolicyExtensionsTests
{
    [Fact]
    public async Task AddAuthorisationPolicy_WhenRegistered_RegistersPolicyWithCorrectClaims()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddAuthorisationPolicy("TestPolicy", "genai-req.write", "genai-req.admin");
        });

        var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync("TestPolicy");

        Assert.NotNull(policy);
        Assert.Single(policy!.AuthenticationSchemes);
        Assert.Contains("Bearer", policy.AuthenticationSchemes);
    }

    [Fact]
    public async Task AddAuthorisationPolicy_WhenRegistered_RequiresAuthenticatedUser()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddAuthorisationPolicy("TestPolicy", "genai-req.read");
        });

        var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync("TestPolicy");

        Assert.NotNull(policy);
        Assert.Contains(policy!.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task AddAuthorisationPolicy_WhenRegistered_RequiresAuthorizationsClaim()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddAuthorisationPolicy("TestPolicy", "genai-req.write");
        });

        var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync("TestPolicy");

        Assert.NotNull(policy);
        var claimRequirement = policy!.Requirements.OfType<ClaimsAuthorizationRequirement>().SingleOrDefault();
        Assert.NotNull(claimRequirement);
        Assert.Equal("authorizations", claimRequirement!.ClaimType);
        Assert.Contains("genai-req.write", claimRequirement.AllowedValues!);
    }

    [Fact]
    public async Task AddAuthorisationPolicy_WhenMultipleScopes_AcceptsMultipleScopes()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddAuthorisationPolicy("ReadPolicy",
                AuthorisationScopes.Read,
                AuthorisationScopes.Write,
                AuthorisationScopes.Admin);
        });

        var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync("ReadPolicy");
        var claimRequirement = policy!.Requirements.OfType<ClaimsAuthorizationRequirement>().Single();

        Assert.Equal(3, claimRequirement.AllowedValues!.Count());
        Assert.Contains("genai-req.read", claimRequirement.AllowedValues!);
        Assert.Contains("genai-req.write", claimRequirement.AllowedValues!);
        Assert.Contains("genai-req.admin", claimRequirement.AllowedValues!);
    }
}
