using Genesis.AI.Api.Swagger;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Genesis.AI.Tests.Swagger;

public class SwaggerEndpointFilterTests
{
    private static DocumentFilterContext EmptyContext() => new(apiDescriptions: [], schemaGenerator: null, schemaRepository: null);

    [Fact]
    public void Apply_OnAnyDocument_AddsSwaggerWildcardPath()
    {
        var document = new OpenApiDocument { Paths = new OpenApiPaths() };
        var filter = new SwaggerEndpointFilter();

        filter.Apply(document, EmptyContext());

        Assert.True(document.Paths.ContainsKey("/swagger/*"));
    }

    [Fact]
    public void Apply_WhenSwaggerPathsAlreadyPresent_ReplacesThemWithSingleWildcard()
    {
        var document = new OpenApiDocument { Paths = new OpenApiPaths() };
        document.Paths.Add("/swagger/v1/swagger.json", new OpenApiPathItem());
        var filter = new SwaggerEndpointFilter();

        filter.Apply(document, EmptyContext());

        Assert.False(document.Paths.ContainsKey("/swagger/v1/swagger.json"));
        Assert.True(document.Paths.ContainsKey("/swagger/*"));
    }

    [Fact]
    public void Apply_OnWildcardPath_ExposesGetAndOptionsOperations()
    {
        var document = new OpenApiDocument { Paths = new OpenApiPaths() };
        var filter = new SwaggerEndpointFilter();

        filter.Apply(document, EmptyContext());

        var wildcard = document.Paths["/swagger/*"];
        Assert.Contains(OperationType.Get, wildcard.Operations.Keys);
        Assert.Contains(OperationType.Options, wildcard.Operations.Keys);
    }
}
