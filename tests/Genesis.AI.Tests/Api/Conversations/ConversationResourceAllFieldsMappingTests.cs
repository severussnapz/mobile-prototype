using System.Text.Json;
using AutoMapper;
using Genesis.AI.Api.Features.Conversations;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Api.Conversations;

public sealed class ConversationResourceAllFieldsMappingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IMapper _mapper;

    public ConversationResourceAllFieldsMappingTests()
    {
        var mapperConfig = new MapperConfiguration(configuration =>
            configuration.AddProfile<ConversationMappingProfile>());
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public void ConversationResource_AndNestedMessageResources_MapAndSerialiseAllFields()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var stageId = Guid.NewGuid();
        var continuedFromConversationId = Guid.NewGuid();
        var conversation = new Conversation(stageId, totalPhases: 8, timeProvider, requirementId: "REQ-123", continuedFromConversationId: continuedFromConversationId);

        var images = new List<MessageImage>
        {
            new() { Data = "base64-image", MediaType = "image/png" }
        };

        var documents = new List<MessageDocument>
        {
            new() { Data = "base64-document", MediaType = "application/pdf", FileName = "req.pdf" }
        };

        var message = conversation.AddMessage(
            MessageRole.User,
            "User message content",
            tokenCount: 77,
            timeProvider,
            userErn: "ern:emis:user:1",
            givenName: "Ada",
            familyName: "Lovelace",
            images: images,
            documents: documents);

        conversation.RecordTokenUsage(10, 20, 3, 4, timeProvider);
        conversation.RecordTokenUsage(1, 2, 5, 6, timeProvider);
        conversation.EnterCrossCheckMode();
        conversation.Resume(timeProvider);

        // Act
        var resource = _mapper.Map<ConversationResource>(conversation);
        var json = JsonSerializer.Serialize(resource, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        // ConversationResource fields (all 12)
        Assert.True(root.TryGetProperty("id", out var idElement), "id field missing");
        Assert.Equal(conversation.Id, idElement.GetGuid());

        Assert.True(root.TryGetProperty("stageId", out var stageIdElement), "stageId field missing");
        Assert.Equal(conversation.StageId, stageIdElement.GetGuid());

        Assert.True(root.TryGetProperty("projectId", out var projectIdElement), "projectId field missing");
        Assert.Equal(Guid.Empty, projectIdElement.GetGuid());

        Assert.True(root.TryGetProperty("requirementId", out var requirementIdElement), "requirementId field missing");
        Assert.Equal("REQ-123", requirementIdElement.GetString());

        Assert.True(root.TryGetProperty("orchestrationMode", out var orchestrationModeElement), "orchestrationMode field missing");
        Assert.Equal("cross_check", orchestrationModeElement.GetString());

        Assert.True(root.TryGetProperty("status", out var statusElement), "status field missing");
        Assert.Equal("active", statusElement.GetString());

        Assert.True(root.TryGetProperty("messageCount", out var messageCountElement), "messageCount field missing");
        Assert.Equal(conversation.MessageCount, messageCountElement.GetInt32());

        Assert.True(root.TryGetProperty("createdAt", out var createdAtElement), "createdAt field missing");
        Assert.Equal(conversation.CreatedAt, createdAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("resumedAt", out var resumedAtElement), "resumedAt field missing");
        Assert.Equal(conversation.ResumedAt, resumedAtElement.GetDateTimeOffset());

        Assert.True(root.TryGetProperty("continuedFromConversationId", out var continuedFromElement), "continuedFromConversationId field missing");
        Assert.Equal(continuedFromConversationId, continuedFromElement.GetGuid());

        Assert.True(root.TryGetProperty("messages", out var messagesElement), "messages field missing");
        var mappedMessage = messagesElement.EnumerateArray().Single();

        Assert.True(root.TryGetProperty("tokenUsage", out var tokenUsageElement), "tokenUsage field missing");

        // MessageResource fields (all 9)
        Assert.True(mappedMessage.TryGetProperty("id", out var messageIdElement), "messages.id field missing");
        Assert.Equal(message.Id, messageIdElement.GetGuid());

        Assert.True(mappedMessage.TryGetProperty("role", out var roleElement), "messages.role field missing");
        Assert.Equal("user", roleElement.GetString());

        Assert.True(mappedMessage.TryGetProperty("content", out var contentElement), "messages.content field missing");
        Assert.Equal(message.Content, contentElement.GetString());

        Assert.True(mappedMessage.TryGetProperty("tokenCount", out var tokenCountElement), "messages.tokenCount field missing");
        Assert.Equal(message.TokenCount, tokenCountElement.GetInt32());

        Assert.True(mappedMessage.TryGetProperty("givenName", out var givenNameElement), "messages.givenName field missing");
        Assert.Equal(message.GivenName, givenNameElement.GetString());

        Assert.True(mappedMessage.TryGetProperty("familyName", out var familyNameElement), "messages.familyName field missing");
        Assert.Equal(message.FamilyName, familyNameElement.GetString());

        Assert.True(mappedMessage.TryGetProperty("createdAt", out var messageCreatedAtElement), "messages.createdAt field missing");
        Assert.Equal(message.CreatedAt, messageCreatedAtElement.GetDateTimeOffset());

        Assert.True(mappedMessage.TryGetProperty("images", out var imagesElement), "messages.images field missing");
        Assert.True(mappedMessage.TryGetProperty("documents", out var documentsElement), "messages.documents field missing");

        // MessageImageResource fields (all 2)
        var mappedImage = imagesElement.EnumerateArray().Single();
        Assert.True(mappedImage.TryGetProperty("data", out var imageDataElement), "messages.images.data field missing");
        Assert.Equal("base64-image", imageDataElement.GetString());
        Assert.True(mappedImage.TryGetProperty("mediaType", out var imageMediaTypeElement), "messages.images.mediaType field missing");
        Assert.Equal("image/png", imageMediaTypeElement.GetString());

        // MessageDocumentResource fields (all 3)
        var mappedDocument = documentsElement.EnumerateArray().Single();
        Assert.True(mappedDocument.TryGetProperty("data", out var documentDataElement), "messages.documents.data field missing");
        Assert.Equal("base64-document", documentDataElement.GetString());
        Assert.True(mappedDocument.TryGetProperty("mediaType", out var documentMediaTypeElement), "messages.documents.mediaType field missing");
        Assert.Equal("application/pdf", documentMediaTypeElement.GetString());
        Assert.True(mappedDocument.TryGetProperty("fileName", out var fileNameElement), "messages.documents.fileName field missing");
        Assert.Equal("req.pdf", fileNameElement.GetString());

        // TokenUsageSummaryResource fields (all 5)
        Assert.True(tokenUsageElement.TryGetProperty("totalInputTokens", out var totalInputTokensElement), "tokenUsage.totalInputTokens field missing");
        Assert.Equal(11, totalInputTokensElement.GetInt32());

        Assert.True(tokenUsageElement.TryGetProperty("totalOutputTokens", out var totalOutputTokensElement), "tokenUsage.totalOutputTokens field missing");
        Assert.Equal(22, totalOutputTokensElement.GetInt32());

        Assert.True(tokenUsageElement.TryGetProperty("totalCacheReadTokens", out var totalCacheReadTokensElement), "tokenUsage.totalCacheReadTokens field missing");
        Assert.Equal(8, totalCacheReadTokensElement.GetInt32());

        Assert.True(tokenUsageElement.TryGetProperty("totalCacheWriteTokens", out var totalCacheWriteTokensElement), "tokenUsage.totalCacheWriteTokens field missing");
        Assert.Equal(10, totalCacheWriteTokensElement.GetInt32());

        Assert.True(tokenUsageElement.TryGetProperty("turnCount", out var turnCountElement), "tokenUsage.turnCount field missing");
        Assert.Equal(2, turnCountElement.GetInt32());
    }
}
