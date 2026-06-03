namespace Genesis.AI.Api.Features.Conversations;

public sealed class CreateParkingLotItemRequest
{
    public string Content { get; init; } = null!;
    public string Priority { get; init; } = "medium";
}
