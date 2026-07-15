using Genesis.AI.Api.Swagger;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Genesis.AI.Tests.Swagger;

public class CorsOptionsOperationFilterTests
{
    private static DocumentFilterContext EmptyContext() => new(apiDescriptions: [], schemaGenerator: null, schemaRepository: null);

    private static OpenApiDocument DocumentWith(string path, OpenApiPathItem item)
    {
        var document = new OpenApiDocument { Paths = new OpenApiPaths() };
        document.Paths.Add(path, item);
        return document;
    }

    [Fact]
    public void Apply_OnPathWithoutOptions_AddsOptionsOperation()
    {
        var item = new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> { [OperationType.Get] = new() }
        };
        var document = DocumentWith("/api/v1/projects", item);

        new CorsOptionsOperationFilter().Apply(document, EmptyContext());

        Assert.Contains(OperationType.Options, document.Paths["/api/v1/projects"].Operations.Keys);
    }

    [Fact]
    public void Apply_OnOptionsOperation_ReturnsNoContentResponse()
    {
        var item = new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation> { [OperationType.Get] = new() }
        };
        var document = DocumentWith("/api/v1/projects", item);

        new CorsOptionsOperationFilter().Apply(document, EmptyContext());

        var options = document.Paths["/api/v1/projects"].Operations[OperationType.Options];
        Assert.True(options.Responses.ContainsKey("204"));
    }

    [Fact]
    public void Apply_WhenOptionsAlreadyPresent_DoesNotOverwriteIt()
    {
        var existingOptions = new OpenApiOperation { Summary = "original" };
        var item = new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new(),
                [OperationType.Options] = existingOptions
            }
        };
        var document = DocumentWith("/api/v1/projects", item);

        new CorsOptionsOperationFilter().Apply(document, EmptyContext());

        Assert.Same(existingOptions, document.Paths["/api/v1/projects"].Operations[OperationType.Options]);
    }
}
