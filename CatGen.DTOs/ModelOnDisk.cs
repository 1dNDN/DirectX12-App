namespace CatGen.DTOs;

public record ModelOnDisk(string FilePath, string Id)
{
    public string FilePath { get; init; } = FilePath;

    public string Filename => Path.GetFileName(FilePath);

    public string Id { get; init; } = Id;
}
