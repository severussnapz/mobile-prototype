using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Commands.ApproveRequirementChange;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Genesis.AI.Domain.Commands.RecordDomainReview;
using Genesis.AI.Domain.Commands.RejectRequirementChange;
using Genesis.AI.Domain.Commands.ReopenStageForAmendment;
using Genesis.AI.Domain.Commands.UndoApproveRequirementChange;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersIRequirementChangeRepository()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IRequirementChangeRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_RegistersIChangeFileWriterService()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IChangeFileWriterService));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_RegistersIContractValidationService()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IContractValidationService));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_RegistersIPipelineReadinessService()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineReadinessService));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_RegistersProposeRequirementChangeCommandHandler()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ProposeRequirementChangeCommandHandler));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_RegistersApproveRequirementChangeCommandHandler()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ApproveRequirementChangeCommandHandler));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_RegistersUndoApproveRequirementChangeCommandHandler()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(UndoApproveRequirementChangeCommandHandler));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_RegistersRejectRequirementChangeCommandHandler()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(RejectRequirementChangeCommandHandler));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_RegistersRecordDomainReviewCommandHandler()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(RecordDomainReviewCommandHandler));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_RegistersReopenStageForAmendmentCommandHandler()
    {
        var services = BuildServiceCollection();
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ReopenStageForAmendmentCommandHandler));
        Assert.NotNull(descriptor);
    }

    private static ServiceCollection BuildServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
                ["S3:ServiceUrl"] = "http://localhost:4566"
            })
            .Build();

        services.AddInfrastructure(configuration);
        return services;
    }
}
