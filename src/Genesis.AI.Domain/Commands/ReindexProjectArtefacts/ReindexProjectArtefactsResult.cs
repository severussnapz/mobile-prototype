namespace Genesis.AI.Domain.Commands.ReindexProjectArtefacts;

public record ReindexProjectArtefactsResult(int Indexed, int Skipped, int Failed);
