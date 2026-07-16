using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Time.Testing;

namespace Genesis.AI.Tests.Infrastructure;

public sealed class HelpChatStreamServiceRetrievalQueryTests
{
    [Fact]
    public void NoPriorMessages_ReturnsCurrentMessageUnchanged()
    {
        var conversation = HelpConversation.Create(Guid.NewGuid(), "user-1", TimeProvider.System);

        var result = HelpChatStreamService.BuildRetrievalQuery(conversation, "how does P06 work");

        Assert.Equal("how does P06 work", result);
    }

    [Fact]
    public void PriorUserMessage_PrependsItWithColonSeparator()
    {
        var conversation = HelpConversation.Create(Guid.NewGuid(), "user-1", TimeProvider.System);
        var timeProvider = new FakeTimeProvider();

        conversation.AddMessage("user", "Artefact Scope Restructure — Solution Design", timeProvider);
        conversation.AddMessage("assistant", "That is correct.", timeProvider);

        var result = HelpChatStreamService.BuildRetrievalQuery(conversation, "why are we doing it");

        Assert.Equal("Artefact Scope Restructure — Solution Design: why are we doing it", result);
    }

    [Fact]
    public void MultiplePriorTurns_UsesMostRecentUserMessageOnly()
    {
        var conversation = HelpConversation.Create(Guid.NewGuid(), "user-1", TimeProvider.System);
        var timeProvider = new FakeTimeProvider();

        conversation.AddMessage("user", "first question", timeProvider);
        conversation.AddMessage("assistant", "a1", timeProvider);
        conversation.AddMessage("user", "second question", timeProvider);
        conversation.AddMessage("assistant", "a2", timeProvider);

        var result = HelpChatStreamService.BuildRetrievalQuery(conversation, "and why");

        Assert.Equal("second question: and why", result);
        Assert.DoesNotContain("first question", result);
    }

    [Fact]
    public void PriorAssistantMessageOnly_ReturnsCurrentMessageUnchanged()
    {
        var conversation = HelpConversation.Create(Guid.NewGuid(), "user-1", TimeProvider.System);
        var timeProvider = new FakeTimeProvider();

        conversation.AddMessage("assistant", "Some answer", timeProvider);

        var result = HelpChatStreamService.BuildRetrievalQuery(conversation, "why");

        Assert.Equal("why", result);
    }

    [Fact]
    public void UsesCreatedAtForOrdering_NotInsertionOrder()
    {
        // Add messages in reverse of what we want to prove — insertion order is earlier-then-later,
        // but CreatedAt timestamps are set so "later question" has the higher timestamp.
        // FakeTimeProvider only moves forward, so we add the earlier-timestamped message first.
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(baseTime);
        var conversation = HelpConversation.Create(Guid.NewGuid(), "user-1", TimeProvider.System);

        // t+1 — inserted first, lower timestamp
        timeProvider.SetUtcNow(baseTime.AddMinutes(1));
        conversation.AddMessage("user", "earlier question", timeProvider);

        // t+2 — inserted second, higher timestamp — must win
        timeProvider.SetUtcNow(baseTime.AddMinutes(2));
        conversation.AddMessage("user", "later question", timeProvider);

        var result = HelpChatStreamService.BuildRetrievalQuery(conversation, "and why");

        Assert.Equal("later question: and why", result);
        Assert.DoesNotContain("earlier question", result);
    }
}
