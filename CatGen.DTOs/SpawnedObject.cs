namespace CatGen.DTOs;

public record SpawnedObject(string Id, string ModelOnDiskId, float X, float Y, float Z)
{
    public string Id { get; init; }= Id;

    public string ModelOnDiskId { get; set; } = ModelOnDiskId;

    public float X { get; init; } = X;

    public float Y { get; init; } = Y;

    public float Z { get; init; } = Z;
}
