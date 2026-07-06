namespace Genesis.AI.Api.Features.Artefacts;

public class RestoreArtefactVersionRequest
{
    public string FilePath { get; set; } = string.Empty;
    public int Version { get; set; }
}
