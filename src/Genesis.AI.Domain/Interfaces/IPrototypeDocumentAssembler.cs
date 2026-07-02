namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Inlines <c>emis-x-base.css</c> into the <c>&lt;head&gt;</c> of a raw HTML
/// document produced by the prototype-demo generation service.
///
/// Single authoritative assembly step shared by the synchronous command handler
/// (via <c>IPrototypeDemoGenerationService.GenerateAsync</c>) and the SSE
/// streaming endpoint. Neither path may duplicate this logic.
/// </summary>
public interface IPrototypeDocumentAssembler
{
    string Assemble(string rawHtml);
}
