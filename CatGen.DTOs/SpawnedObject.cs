namespace CatGen.DTOs;

public record SpawnedObject(string Id, string ModelOnDiskId, float X, float Y, float Z, string Name)
{
    public string Id { get; init; }= Id;

    public string ModelOnDiskId { get; set; } = ModelOnDiskId;

    public float X { get; set; } = X;

    public float Y { get; set; } = Y;

    public float Z { get; set; } = Z;

    public string Name { get; set; } = Name;
}
