namespace Genesis.AI.Api.Dtos;

public sealed class CreateParkingLotItemRequest
{
    public string Content { get; init; } = null!;
    public string Priority { get; init; } = "medium";
}
