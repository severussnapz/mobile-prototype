using Genesis.AI.Api.Features.PipelineReadiness;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class PipelineReadinessControllerTests
{
    [Fact]
    public void PipelineReadinessController_HasCorrectRoute()
    {
        var routeAttr = typeof(PipelineReadinessController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), false)
            .FirstOrDefault() as Microsoft.AspNetCore.Mvc.RouteAttribute;

        Assert.NotNull(routeAttr);
        Assert.Contains("pipeline-readiness", routeAttr!.Template);
    }

    [Fact]
    public void PipelineReadinessResponse_WhenReady_IsReadyTrue()
    {
        var response = new PipelineReadinessResponse(
            IsReady: true,
            Blockers: []);

        Assert.True(response.IsReady);
        Assert.Empty(response.Blockers);
    }

    [Fact]
    public void PipelineReadinessResponse_WhenBlocked_ContainsBlockers()
    {
        var response = new PipelineReadinessResponse(
            IsReady: false,
            Blockers: ["CHANGE-001: IG review outstanding"]);

        Assert.False(response.IsReady);
        Assert.Single(response.Blockers);
    }
}
