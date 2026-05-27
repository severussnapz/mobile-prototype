using Genesis.AI.Api.Authentication;
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

// Health checks (OBS-004)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

// Authentication (AUTH-005, AUTH-006)
var jwtAuthority = builder.Configuration["Authentication:Authority"];
var jwtAudience = builder.Configuration["Authentication:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrEmpty(jwtAuthority))
            options.Authority = jwtAuthority;
        if (!string.IsNullOrEmpty(jwtAudience))
            options.Audience = jwtAudience;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwtAuthority),
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
});

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Genesis.AI.Domain.AssemblyMarker>());

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Infrastructure (EF Core + Npgsql)
builder.Services.AddInfrastructure(builder.Configuration);

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
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
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
