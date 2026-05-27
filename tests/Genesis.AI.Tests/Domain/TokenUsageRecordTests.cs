using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Domain;

public class TokenUsageRecordTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void RecordTokenUsage_WhenCalled_CreatesRecordWithCorrectValues()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        var record = conversation.RecordTokenUsage(1500, 3200, 800, 200, _timeProvider);

        Assert.NotNull(record);
        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(conversation.Id, record.ConversationId);
        Assert.Equal(1500, record.InputTokens);
        Assert.Equal(3200, record.OutputTokens);
        Assert.Equal(800, record.CacheReadInputTokens);
        Assert.Equal(200, record.CacheWriteInputTokens);
    }

    [Fact]
    public void RecordTokenUsage_WhenCalled_SetsCreatedAtTimestamp()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        var record = conversation.RecordTokenUsage(100, 200, 0, 0, _timeProvider);

        Assert.True(record.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(record.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void RecordTokenUsage_MultipleCalls_AddsMultipleRecords()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        conversation.RecordTokenUsage(100, 200, 0, 0, _timeProvider);
        conversation.RecordTokenUsage(300, 400, 50, 25, _timeProvider);
        conversation.RecordTokenUsage(500, 600, 100, 50, _timeProvider);

        Assert.Equal(3, conversation.TokenUsageRecords.Count);
    }

    [Fact]
    public void RecordTokenUsage_WithZeroValues_CreatesRecordSuccessfully()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        var record = conversation.RecordTokenUsage(0, 0, 0, 0, _timeProvider);

        Assert.NotNull(record);
        Assert.Equal(0, record.InputTokens);
        Assert.Equal(0, record.OutputTokens);
        Assert.Equal(0, record.CacheReadInputTokens);
        Assert.Equal(0, record.CacheWriteInputTokens);
    }

    [Fact]
    public void RecordTokenUsage_ReturnsRecord_WithUniqueIds()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);

        var record1 = conversation.RecordTokenUsage(100, 200, 0, 0, _timeProvider);
        var record2 = conversation.RecordTokenUsage(300, 400, 0, 0, _timeProvider);

        Assert.NotEqual(record1.Id, record2.Id);
    }
}
