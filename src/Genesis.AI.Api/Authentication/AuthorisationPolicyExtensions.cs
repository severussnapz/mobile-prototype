using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Genesis.AI.Api.Authentication;

public static class AuthorisationPolicyExtensions
{
    public static AuthorizationOptions AddAuthorisationPolicy(
        this AuthorizationOptions options, string name, params string[] claims)
    {
        options.AddPolicy(name, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("authorizations", claims);
        });
        return options;
    }
}
