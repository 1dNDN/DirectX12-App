// ReSharper disable All
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace CatGen.DTOs;

public record ModelOnDisk(string FilePath, string Id)
{
    public string FilePath { get; init; } = FilePath;

    public string Filename => Path.GetFileName(FilePath);

    public string Id { get; init; } = Id;

    public List<SpawnedEntityMetadata> SpawnedObjects { get; }
}
