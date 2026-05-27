using Genesis.AI.Domain.Commands.AddParkingLotItem;

namespace Genesis.AI.Tests.Validators;

public class AddParkingLotItemCommandValidatorTests
{
    private readonly AddParkingLotItemCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new AddParkingLotItemCommand(Guid.NewGuid(), "Investigate API auth flow", "high");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyConversationId_ShouldFail()
    {
        var command = new AddParkingLotItemCommand(Guid.Empty, "Content", "high");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ConversationId");
    }

    [Fact]
    public void Validate_EmptyContent_ShouldFail()
    {
        var command = new AddParkingLotItemCommand(Guid.NewGuid(), "", "high");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Content");
    }

    [Fact]
    public void Validate_ContentTooLong_ShouldFail()
    {
        var command = new AddParkingLotItemCommand(Guid.NewGuid(), new string('x', 2001), "high");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Content");
    }

    [Fact]
    public void Validate_ContentAtMaxLength_ShouldPass()
    {
        var command = new AddParkingLotItemCommand(Guid.NewGuid(), new string('x', 2000), "high");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyPriority_ShouldFail()
    {
        var command = new AddParkingLotItemCommand(Guid.NewGuid(), "Content", "");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Priority");
    }
}
