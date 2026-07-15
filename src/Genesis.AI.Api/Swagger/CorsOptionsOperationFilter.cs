using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Genesis.AI.Api.Swagger;

/// <summary>
/// Declares an OPTIONS operation on every path so CORS pre-flight requests are
/// part of the OpenAPI document. APIM only forwards operations that appear in
/// the imported specification, so without this the gateway rejects browser
/// pre-flight (OPTIONS) requests and cross-origin calls from the frontend fail.
/// </summary>
public sealed class CorsOptionsOperationFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var path in swaggerDoc.Paths)
        {
            // Leave paths that already declare OPTIONS untouched (e.g. the
            // /swagger/* wildcard added by SwaggerEndpointFilter).
            if (path.Value.Operations.ContainsKey(OperationType.Options))
            {
                continue;
            }

            // Reuse the path parameters from an existing operation so the OPTIONS
            // entry stays a valid OpenAPI operation for the same route.
            var existingOperation = path.Value.Operations.Values.FirstOrDefault();
            var pathParameters = existingOperation?.Parameters?
                .Where(parameter => parameter.In == ParameterLocation.Path)
                .ToList() ?? [];

            path.Value.Operations[OperationType.Options] = new OpenApiOperation
            {
                Tags = existingOperation?.Tags ?? [],
                Summary = "CORS pre-flight request",
                Description = "Handles CORS pre-flight requests for this endpoint.",
                Parameters = pathParameters,
                Responses = new OpenApiResponses
                {
                    ["204"] = new OpenApiResponse
                    {
                        Description = "No Content - CORS pre-flight successful.",
                        Headers = new Dictionary<string, OpenApiHeader>
                        {
                            ["Access-Control-Allow-Origin"] = new OpenApiHeader
                            {
                                Description = "Allowed origins",
                                Schema = new OpenApiSchema { Type = "string" }
                            },
                            ["Access-Control-Allow-Methods"] = new OpenApiHeader
                            {
                                Description = "Allowed HTTP methods",
                                Schema = new OpenApiSchema { Type = "string" }
                            },
                            ["Access-Control-Allow-Headers"] = new OpenApiHeader
                            {
                                Description = "Allowed headers",
                                Schema = new OpenApiSchema { Type = "string" }
                            }
                        }
                    }
                }
            };
        }
    }
}
