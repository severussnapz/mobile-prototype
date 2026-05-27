using Genesis.AI.Domain.Commands.CreateConversation;
using Genesis.AI.Domain.Commands.SendMessage;
using Genesis.AI.Domain.Commands.CompleteStage;
using Genesis.AI.Domain.Commands.SkipStage;
using Genesis.AI.Domain.Commands.DeleteProject;

namespace Genesis.AI.Tests.Validators;

public class CommandValidatorTests
{
    // ========================================================================
    // CreateConversationCommandValidator
    // ========================================================================

    [Fact]
    public void CreateConversation_ValidStageId_ShouldPass()
    {
        var validator = new CreateConversationCommandValidator();
        var command = new CreateConversationCommand(Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateConversation_EmptyStageId_ShouldFail()
    {
        var validator = new CreateConversationCommandValidator();
        var command = new CreateConversationCommand(Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StageId");
    }

    // ========================================================================
    // SendMessageCommandValidator
    // ========================================================================

    [Fact]
    public void SendMessage_ValidCommand_ShouldPass()
    {
        var validator = new SendMessageCommandValidator();
        var command = new SendMessageCommand(Guid.NewGuid(), "Hello, world!", "user-1");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SendMessage_EmptyConversationId_ShouldFail()
    {
        var validator = new SendMessageCommandValidator();
        var command = new SendMessageCommand(Guid.Empty, "Hello", "user-1");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ConversationId");
    }

    [Fact]
    public void SendMessage_EmptyContent_ShouldFail()
    {
        var validator = new SendMessageCommandValidator();
        var command = new SendMessageCommand(Guid.NewGuid(), "", "user-1");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Content");
    }

    [Fact]
    public void SendMessage_EmptyUserId_ShouldFail()
    {
        var validator = new SendMessageCommandValidator();
        var command = new SendMessageCommand(Guid.NewGuid(), "Hello", "");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UserId");
    }

    // ========================================================================
    // CompleteStageCommandValidator
    // ========================================================================

    [Fact]
    public void CompleteStage_ValidCommand_ShouldPass()
    {
        var validator = new CompleteStageCommandValidator();
        var command = new CompleteStageCommand(Guid.NewGuid(), "user-1");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CompleteStage_EmptyStageId_ShouldFail()
    {
        var validator = new CompleteStageCommandValidator();
        var command = new CompleteStageCommand(Guid.Empty, "user-1");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StageId");
    }

    [Fact]
    public void CompleteStage_EmptyUserId_ShouldFail()
    {
        var validator = new CompleteStageCommandValidator();
        var command = new CompleteStageCommand(Guid.NewGuid(), "");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UserId");
    }

    // ========================================================================
    // SkipStageCommandValidator
    // ========================================================================

    [Fact]
    public void SkipStage_ValidCommand_ShouldPass()
    {
        var validator = new SkipStageCommandValidator();
        var command = new SkipStageCommand(Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SkipStage_EmptyStageId_ShouldFail()
    {
        var validator = new SkipStageCommandValidator();
        var command = new SkipStageCommand(Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StageId");
    }

    // ========================================================================
    // DeleteProjectCommandValidator
    // ========================================================================

    [Fact]
    public void DeleteProject_ValidCommand_ShouldPass()
    {
        var validator = new DeleteProjectCommandValidator();
        var command = new DeleteProjectCommand(Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DeleteProject_EmptyProjectId_ShouldFail()
    {
        var validator = new DeleteProjectCommandValidator();
        var command = new DeleteProjectCommand(Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ProjectId");
    }
}
