namespace CatGen.DTOs;

public record ModelOnDisk(string FilePath)
{
    public string FilePath { get; init; } = FilePath;

    public string Filename => Path.GetFileName(FilePath);
}
