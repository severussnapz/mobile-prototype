using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Xunit;

namespace Genesis.AI.Tests.Domain;

/// <summary>
/// Structural tests confirming the interface contract is correctly defined.
/// Integration tests cover actual repository behaviour against the DB.
/// </summary>
public class IRequirementChangeRepositoryTests
{
    [Fact]
    public void IRequirementChangeRepository_HasAddAsync()
    {
        var method = typeof(IRequirementChangeRepository)
            .GetMethod("AddAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IRequirementChangeRepository_HasGetByIdAsync()
    {
        var method = typeof(IRequirementChangeRepository)
            .GetMethod("GetByIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IRequirementChangeRepository_HasGetByProjectIdAsync()
    {
        var method = typeof(IRequirementChangeRepository)
            .GetMethod("GetByProjectIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IRequirementChangeRepository_HasGetPendingByProjectIdAsync()
    {
        var method = typeof(IRequirementChangeRepository)
            .GetMethod("GetPendingByProjectIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IRequirementChangeRepository_HasHasOpenDefiniteReviewsAsync()
    {
        var method = typeof(IRequirementChangeRepository)
            .GetMethod("HasOpenDefiniteReviewsAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IRequirementChangeRepository_HasUnitOfWork()
    {
        var property = typeof(IRequirementChangeRepository)
            .GetProperty("UnitOfWork");
        Assert.NotNull(property);
    }
}
