using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Domain;

public class ConversationTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void Constructor_WhenCreated_SetsCorrectProperties()
    {
        var stageId = Guid.NewGuid();

        var conversation = new Conversation(stageId, 5, _timeProvider);

        Assert.NotEqual(Guid.Empty, conversation.Id);
        Assert.Equal(stageId, conversation.StageId);
        Assert.Equal(ConversationStatus.Active, conversation.Status);
        Assert.Equal(0, conversation.MessageCount);
        Assert.Equal(0, conversation.CurrentPhase);
        Assert.Equal(5, conversation.TotalPhases);
        Assert.Equal(0, conversation.QuestionsAsked);
    }

    [Fact]
    public void AddMessage_UserRole_IncrementsQuestionsAsked()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.AddMessage(MessageRole.User, "Hello", null, _timeProvider);

        Assert.Equal(1, conversation.QuestionsAsked);
    }

    [Fact]
    public void AddMessage_AssistantRole_DoesNotIncrementQuestionsAsked()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.AddMessage(MessageRole.Assistant, "Response", null, _timeProvider);

        Assert.Equal(0, conversation.QuestionsAsked);
    }

    [Fact]
    public void AddMessage_WhenCalled_IncrementsMessageCount()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.AddMessage(MessageRole.User, "Hello", null, _timeProvider);
        conversation.AddMessage(MessageRole.Assistant, "Response", 100, _timeProvider);

        Assert.Equal(2, conversation.MessageCount);
    }

    [Fact]
    public void AddMessage_WhenCalled_ReturnsMessage()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        var message = conversation.AddMessage(MessageRole.User, "Hello AI", null, _timeProvider);

        Assert.NotNull(message);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("Hello AI", message.Content);
    }

    [Fact]
    public void AdvancePhase_WhenCalled_IncrementsCurrentPhase()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.AdvancePhase("context_gathering");

        Assert.Equal(1, conversation.CurrentPhase);
        Assert.Equal("context_gathering", conversation.PhaseName);
    }

    [Fact]
    public void AdvancePhase_MultipleCalls_IncrementsCumulatively()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.AdvancePhase("phase_1");
        conversation.AdvancePhase("phase_2");
        conversation.AdvancePhase("phase_3");

        Assert.Equal(3, conversation.CurrentPhase);
        Assert.Equal("phase_3", conversation.PhaseName);
    }

    [Fact]
    public void SetPhase_WhenCalled_SetsPhaseDirectly()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.SetPhase(3, "validation");

        Assert.Equal(3, conversation.CurrentPhase);
        Assert.Equal("validation", conversation.PhaseName);
    }

    [Fact]
    public void SetEstimatedQuestions_WhenCalled_SetsValue()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.SetEstimatedQuestions(15);

        Assert.Equal(15, conversation.EstimatedTotalQuestions);
    }

    [Fact]
    public void UpdateProgress_WhenCalled_UpdatesAllFields()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.UpdateProgress(10, 20, 5);

        Assert.Equal(10, conversation.QuestionsAsked);
        Assert.Equal(20, conversation.EstimatedTotalQuestions);
        Assert.Equal(5, conversation.RequirementsCaptured);
    }

    [Fact]
    public void UpdateProgress_NullRequirementsCaptured_DoesNotUpdate()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        conversation.UpdateProgress(5, 10, 3);

        conversation.UpdateProgress(8, 15, null);

        Assert.Equal(3, conversation.RequirementsCaptured);
    }

    [Fact]
    public void AddParkingLotItem_WhenCalled_AddsItemToCollection()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        var item = conversation.AddParkingLotItem("Review auth", ParkingLotPriority.High, _timeProvider);

        Assert.Single(conversation.ParkingLotItems);
        Assert.Equal("Review auth", item.Content);
        Assert.Equal(ParkingLotPriority.High, item.Priority);
    }

    [Fact]
    public void Complete_WhenActive_SetsStatusToCompleted()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.Complete();

        Assert.Equal(ConversationStatus.Completed, conversation.Status);
    }

    [Fact]
    public void Pause_WhenActive_SetsStatusToPaused()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.Pause();

        Assert.Equal(ConversationStatus.Paused, conversation.Status);
    }

    [Fact]
    public void Resume_WhenPaused_SetsStatusToActive()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        conversation.Pause();

        conversation.Resume(_timeProvider);

        Assert.Equal(ConversationStatus.Active, conversation.Status);
        Assert.NotNull(conversation.ResumedAt);
    }
}
