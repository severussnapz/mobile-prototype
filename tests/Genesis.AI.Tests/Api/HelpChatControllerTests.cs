using System.Text;
using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Genesis.AI.Tests.Api;

public class HelpChatControllerTests
{
    [Fact]
    public async Task GetConversation_WhenConversationExists_ReturnsConversationId()
    {
        // GET /api/v1/help/conversations?projectId={id}
        var projectId = Guid.NewGuid();
        var conversation = HelpConversation.Create(projectId, "user-ern", TimeProvider.System);

        var repository = new Mock<IHelpConversationRepository>();
        repository
            .Setup(mock => mock.GetMostRecentByUserAndProjectAsync(It.IsAny<string>(), projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var streamService = new Mock<IHelpChatStreamService>();
        var controller = new HelpChatController(repository.Object, streamService.Object);

        ActionResult<HelpConversationResponse?> actionResult = await controller.GetConversation(projectId, CancellationToken.None);

        Assert.NotNull(actionResult.Value);
        Assert.Equal(conversation.Id, actionResult.Value!.Id);
    }

    [Fact]
    public async Task GetConversation_WhenNoConversationExists_ReturnsNull()
    {
        var projectId = Guid.NewGuid();

        var repository = new Mock<IHelpConversationRepository>();
        repository
            .Setup(mock => mock.GetMostRecentByUserAndProjectAsync(It.IsAny<string>(), projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HelpConversation?)null);

        var streamService = new Mock<IHelpChatStreamService>();
        var controller = new HelpChatController(repository.Object, streamService.Object);

        ActionResult<HelpConversationResponse?> actionResult = await controller.GetConversation(projectId, CancellationToken.None);

        Assert.Null(actionResult.Value);
    }

    [Fact]
    public async Task Stream_WhenNewConversation_CreatesAndStreams()
    {
        // POST /api/v1/help/stream
        var repository = new Mock<IHelpConversationRepository>();
        repository
            .Setup(mock => mock.GetMostRecentByUserAndProjectAsync(It.IsAny<string>(), (Guid?)null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HelpConversation?)null);

        var streamService = new Mock<IHelpChatStreamService>();
        streamService
            .Setup(mock => mock.StreamAsync(It.IsAny<HelpStreamRequest>(), It.IsAny<CancellationToken>()))
            .Returns(CreateStream("P06 is the Clinical Safety stage"));

        var controller = new HelpChatController(repository.Object, streamService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.HttpContext.Response.Body = new MemoryStream();

        await controller.Stream(
            new HelpStreamRequest
            {
                Message = "what does P06 do?",
                ProjectId = null
            },
            CancellationToken.None);

        controller.HttpContext.Response.Body.Position = 0;
        var responseText = await new StreamReader(controller.HttpContext.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.Equal("text/event-stream", controller.HttpContext.Response.ContentType);
        Assert.Contains("P06 is the Clinical Safety stage", responseText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stream_WhenExistingConversation_ContinuesAndStreams()
    {
        // POST /api/v1/help/stream
        var existingConversationId = Guid.NewGuid();
        var existingConversation = HelpConversation.Create(null, "user-ern", TimeProvider.System);

        var repository = new Mock<IHelpConversationRepository>();
        repository
            .Setup(mock => mock.GetByIdWithMessagesAsync(existingConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConversation);

        var streamService = new Mock<IHelpChatStreamService>();
        streamService
            .Setup(mock => mock.StreamAsync(It.IsAny<HelpStreamRequest>(), It.IsAny<CancellationToken>()))
            .Returns(CreateStream("Continuing existing conversation"));

        var controller = new HelpChatController(repository.Object, streamService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.HttpContext.Response.Body = new MemoryStream();

        await controller.Stream(
            new HelpStreamRequest
            {
                Message = "continue",
                HelpConversationId = existingConversationId
            },
            CancellationToken.None);

        Assert.Equal("text/event-stream", controller.HttpContext.Response.ContentType);
    }

    [Fact]
    public async Task Stream_WhenMessageIsEmpty_ReturnsBadRequest()
    {
        // POST /api/v1/help/stream
        var repository = new Mock<IHelpConversationRepository>();
        var streamService = new Mock<IHelpChatStreamService>();

        var controller = new HelpChatController(repository.Object, streamService.Object);

        IActionResult result = await controller.Stream(
            new HelpStreamRequest
            {
                Message = string.Empty,
                ProjectId = null
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    private static async IAsyncEnumerable<string> CreateStream(params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return chunk;
            await Task.Yield();
        }
    }
}
