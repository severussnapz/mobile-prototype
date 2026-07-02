using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Genesis.AI.Api.Authentication;
using Genesis.AI.Api.Http;
using Genesis.AI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Api.Features.PrototypeDemo;

/// <summary>
/// SSE streaming endpoint for prototype-demo generation.
///
/// Route path contains <c>/stream</c> so the webpack dev-server proxy sets
/// <c>x-accel-buffering: no</c> and deletes <c>content-encoding</c> via the
/// <c>onProxyRes</c> hook, preventing the proxy from re-buffering the event stream.
///
/// Event contract:
///   event: status   — {"status":"started"|"loading_requirements"|"generating"}
///   event: chunk    — {"text":"..."} — progress display only, not authoritative
///   event: done     — {"html":"<!DOCTYPE html>..."} — fully assembled, CSS-inlined document
///   event: error    — {"code":"...","message":"..."} — generation or timeout failure
///   data: [DONE]    — terminal sentinel (mirrors ConversationStreamController)
///
/// The <c>done</c> event carries the authoritative assembled HTML produced by
/// <see cref="PrototypeDocumentAssembler.Assemble"/> — the same code path used by
/// the synchronous endpoint — so the two endpoints cannot drift.
///
/// The synchronous <c>POST .../prototype-demo</c> endpoint in
/// <see cref="PrototypeDemoController"/> is unchanged.
/// </summary>
[ApiController]
[Route("api/v1/projects/{projectId:guid}/prototype-demo")]
[Authorize(Policy = AuthorisationPolicies.ProjectWrite)]
public sealed class PrototypeDemoStreamController : ControllerBase
{
    private readonly IProjectRepository _projectRepository;
    private readonly IPrototypeDemoGenerationService _generationService;
    private readonly IPrototypeDocumentAssembler _assembler;
    private readonly IPrototypeDemoSettings _settings;
    private readonly ILogger<PrototypeDemoStreamController> _logger;

    public PrototypeDemoStreamController(
        IProjectRepository projectRepository,
        IPrototypeDemoGenerationService generationService,
        IPrototypeDocumentAssembler assembler,
        IPrototypeDemoSettings settings,
        ILogger<PrototypeDemoStreamController> logger)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
        _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task StreamPrototypeDemo(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(
                ApiErrorResponse.Create("404", "Project not found", $"No project found with ID '{projectId}'."),
                cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await WriteStatusEventAsync("started", cancellationToken);
        await WriteStatusEventAsync("loading_requirements", cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_settings.GenerationTimeout);

        var rawHtml = new StringBuilder();
        var firstChunk = true;

        try
        {
            await foreach (var chunk in _generationService.StreamRawAsync(projectId, project.Name, timeoutCts.Token))
            {
                if (firstChunk)
                {
                    firstChunk = false;
                    await WriteStatusEventAsync("generating", cancellationToken);
                }

                rawHtml.Append(chunk);
                var chunkData = JsonSerializer.Serialize(new { text = chunk });
                await Response.WriteAsync($"event: chunk\ndata: {chunkData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            var assembled = _assembler.Assemble(rawHtml.ToString());
            var doneData = JsonSerializer.Serialize(new { html = assembled });
            await Response.WriteAsync($"event: done\ndata: {doneData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            var errorData = JsonSerializer.Serialize(new
            {
                code = "timeout",
                message = $"Prototype generation timed out after {_settings.GenerationTimeout.TotalMinutes:0} minutes."
            });
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Prototype demo generation failed for project {ProjectId}", projectId);
            var errorData = JsonSerializer.Serialize(new { code = "generation_failed", message = "Prototype generation failed. Please try again." });
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", CancellationToken.None);
        await Response.Body.FlushAsync(CancellationToken.None);
    }

    private async Task WriteStatusEventAsync(string status, CancellationToken cancellationToken)
    {
        var statusData = JsonSerializer.Serialize(new { status });
        await Response.WriteAsync($"event: status\ndata: {statusData}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
