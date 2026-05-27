using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetConversation;
using Genesis.AI.Domain.Queries.GetConversationsByStage;
using Moq;

namespace Genesis.AI.Tests.Queries;

public class ConversationQueryHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly TimeProvider _timeProvider;

    public ConversationQueryHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _timeProvider = TimeProvider.System;
    }

    // ========================================================================
    // GetConversationQueryHandler
    // ========================================================================

    [Fact]
    public async Task GetConversation_Exists_ReturnsConversation()
    {
        var conversation = new Conversation(Guid.NewGuid(), 5, _timeProvider);
        _conversationRepositoryMock
            .Setup(r => r.GetByIdWithMessagesAsync(conversation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var handler = new GetConversationQueryHandler(_conversationRepositoryMock.Object);
        var query = new GetConversationQuery(conversation.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(conversation.Id, result.Id);
    }

    [Fact]
    public async Task GetConversation_NotFound_ReturnsNull()
    {
        var conversationId = Guid.NewGuid();
        _conversationRepositoryMock
            .Setup(r => r.GetByIdWithMessagesAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var handler = new GetConversationQueryHandler(_conversationRepositoryMock.Object);
        var query = new GetConversationQuery(conversationId);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }

    // ========================================================================
    // GetConversationsByStageQueryHandler
    // ========================================================================

    [Fact]
    public async Task GetConversationsByStage_WhenConversationsExist_ReturnsConversations()
    {
        var stageId = Guid.NewGuid();
        var conversations = new List<Conversation>
        {
            new(stageId, 5, _timeProvider),
            new(stageId, 5, _timeProvider),
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversations);

        var handler = new GetConversationsByStageQueryHandler(_conversationRepositoryMock.Object);
        var query = new GetConversationsByStageQuery(stageId);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetConversationsByStage_EmptyResult_ReturnsEmptyList()
    {
        var stageId = Guid.NewGuid();
        _conversationRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Conversation>());

        var handler = new GetConversationsByStageQueryHandler(_conversationRepositoryMock.Object);
        var query = new GetConversationsByStageQuery(stageId);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Empty(result);
    }
}
