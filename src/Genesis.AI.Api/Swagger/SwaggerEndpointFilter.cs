using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Genesis.AI.Api.Swagger;

/// <summary>
/// Publishes the Swagger UI and OpenAPI JSON endpoints into the generated
/// OpenAPI document as a single <c>/swagger/*</c> wildcard path. APIM only
/// forwards operations that appear in the imported specification, so without
/// this the public gateway returns 404 for <c>/swagger</c> even though the app
/// serves it. The wildcard lets APIM route every Swagger asset to the backend.
/// </summary>
public sealed class SwaggerEndpointFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Drop any Swagger paths Swashbuckle may have emitted so we control the
        // single wildcard entry.
        var existingSwaggerPaths = swaggerDoc.Paths
            .Where(pathEntry => pathEntry.Key.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            .Select(pathEntry => pathEntry.Key)
            .ToList();

        foreach (var path in existingSwaggerPaths)
        {
            swaggerDoc.Paths.Remove(path);
        }

        // Add the /swagger/* wildcard so APIM path-based routing forwards the UI
        // and the OpenAPI JSON to the backend.
        swaggerDoc.Paths.Add("/swagger/*", new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new() { Name = "Swagger UI" } },
                    Summary = "Swagger UI and OpenAPI specification",
                    Description = "Serves the Swagger UI documentation and the OpenAPI JSON specification.",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Swagger UI HTML or OpenAPI JSON specification."
                        }
                    }
                },
                [OperationType.Options] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new() { Name = "Swagger UI" } },
                    Summary = "CORS pre-flight request",
                    Description = "Handles CORS pre-flight requests for the Swagger UI.",
                    Responses = new OpenApiResponses
                    {
                        ["204"] = new OpenApiResponse
                        {
                            Description = "No Content - CORS pre-flight successful."
                        }
                    }
                }
            }
        });
    }
}
