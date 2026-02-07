using CatGen.DTOs;

namespace CatGen.Saves;

public static class SaveService
{
    private static readonly SaveContext _context = new SaveContext();

    public static List<ModelOnDisk> GetModelsOnDisk()
    {
        return _context.ModelsOnDisk.ToList();
    }

    public static void Save(List<ModelOnDisk> models)
    {
        var oldModels = GetModelsOnDisk();

        _context.ModelsOnDisk.RemoveRange(oldModels);
        _context.ModelsOnDisk.AddRange(models);
        _context.SaveChanges();
    }
}
