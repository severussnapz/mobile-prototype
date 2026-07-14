using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Features.Artefacts;
using Genesis.AI.Api.Health;
using Genesis.AI.Api.Middleware;
using Genesis.AI.Core.Filters;
using Genesis.AI.Core.Logging;
using Genesis.AI.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Suppress Kestrel Server header (SEC-005)
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Serilog (OBS-002)
builder.Host.ConfigureSerilog();

// Health checks (OBS-004). The readiness probe verifies database connectivity
// through the EF Core DbContext, which uses the IAM-authenticated data source
// in AWS and the local connection string in development.
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);

// Authentication (AUTH-005, AUTH-006)
var jwtAuthority = builder.Configuration["Authentication:Authority"];
var jwtAudience = builder.Configuration["Authentication:Audience"];
var hasValidHttpsAuthority = Uri.TryCreate(jwtAuthority, UriKind.Absolute, out var authorityUri)
    && authorityUri.Scheme == Uri.UriSchemeHttps;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (hasValidHttpsAuthority)
            options.Authority = jwtAuthority;
        if (!string.IsNullOrEmpty(jwtAudience))
            options.Audience = jwtAudience;

        // Local development may run without identity wiring; avoid hard-fail when
        // compose resolves empty IDENTITY_URL into a non-HTTPS authority like "/v2.0/".
        if (!hasValidHttpsAuthority && builder.Environment.IsDevelopment())
            options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = hasValidHttpsAuthority,
            ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        options.MapInboundClaims = false;
    });

// Authorisation policies (AUTH-003)
builder.Services.AddAuthorization(options =>
{
    options.AddAuthorisationPolicy(
        AuthorisationPolicies.ProjectRead,
        AuthorisationScopes.Read, AuthorisationScopes.Write, AuthorisationScopes.Admin);

    options.AddAuthorisationPolicy(
        AuthorisationPolicies.ProjectWrite,
        AuthorisationScopes.Write, AuthorisationScopes.Admin);

    options.AddAuthorisationPolicy(
        AuthorisationPolicies.ConversationRead,
        AuthorisationScopes.Read, AuthorisationScopes.Write, AuthorisationScopes.Admin,
        AuthorisationScopes.Architecture, AuthorisationScopes.ProductDesign, AuthorisationScopes.ClinicalSafety);

    options.AddAuthorisationPolicy(
        AuthorisationPolicies.ConversationWrite,
        AuthorisationScopes.Write, AuthorisationScopes.Admin,
        AuthorisationScopes.Architecture, AuthorisationScopes.ProductDesign, AuthorisationScopes.ClinicalSafety);

    options.AddAuthorisationPolicy(
        AuthorisationPolicies.ArchitectureConverse,
        AuthorisationScopes.Architecture, AuthorisationScopes.Admin);

    options.AddAuthorisationPolicy(
        AuthorisationPolicies.ProductDesignConverse,
        AuthorisationScopes.ProductDesign, AuthorisationScopes.Admin);

    options.AddAuthorisationPolicy(
        AuthorisationPolicies.ClinicalSafetyConverse,
        AuthorisationScopes.ClinicalSafety, AuthorisationScopes.Admin);

    options.AddAuthorisationPolicy(
        AuthorisationPolicies.AdminOnly,
        AuthorisationScopes.Admin);
});

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Genesis.AI.Domain.AssemblyMarker>());

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Infrastructure (EF Core + Npgsql)
builder.Services.AddInfrastructure(builder.Configuration);

// API services
builder.Services.AddScoped<IArtefactRestorationService, ArtefactRestorationService>();

// TimeProvider for testable clock
builder.Services.AddSingleton(TimeProvider.System);

// Exception filter (OBS-003)
builder.Services.AddScoped<IExceptionLoggingFilter, ExceptionLoggingFilter>();

// MVC + JSON:API
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<IExceptionLoggingFilter>();
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:8080"];

builder.Services.AddCors(options =>
{
    // guardrail:skip=AUTH-003:This is CorsOptions.AddPolicy (CORS), not AuthorizationOptions.AddPolicy (auth)
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

// Swagger (OpenAPI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Genesis.AI API",
        Version = "v1"
    });
});

var app = builder.Build();

// Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check probes (OBS-004)
// Liveness (/healthz) runs no checks — it only confirms the process is up, so a
// dependency blip cannot trigger a pod restart. Readiness (/healthz/ready) runs
// the registered checks (e.g. PostgreSQL) to gate load-balancer traffic.
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = _ => true
}).AllowAnonymous();

// Security response headers (SEC-005)
app.UseMiddleware<ResponseHeadersMiddleware>();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace Genesis.AI.Api
{
    public sealed class Program;
}
