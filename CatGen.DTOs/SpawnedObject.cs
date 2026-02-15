namespace CatGen.DTOs;

public record SpawnedEntityMetadata
{
    public SpawnedEntityMetadata(string id, string modelOnDiskId, string name)
    {
        Id = id;
        ModelOnDiskId = modelOnDiskId;
        Name = name;
        Scale = 1;
    }

    public string Id { get; set; }

    public string ModelOnDiskId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public string Name { get; set; }

    public float Scale { get; set; }

    public float Yaw { get; set; }

    public float Pitch { get; set; }

    public float Roll { get; set; }
}
