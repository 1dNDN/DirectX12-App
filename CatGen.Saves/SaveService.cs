using CatGen.DTOs;

using Microsoft.EntityFrameworkCore;

namespace CatGen.Saves;

public static class SaveService
{
    static SaveService()
    {
        _сontext = new SaveContext();

        _сontext.Database.Migrate();
    }

    private static readonly SaveContext _сontext;

    // это неправильно, но мне пох
    private static readonly Lock _contextLock = new();

    public static List<ModelOnDisk> GetModelsOnDisk()
    {
        lock (_contextLock)
        {
            return _сontext.ModelsOnDisk.ToList();
        }
    }

    public static List<SpawnedEntityMetadata> GetSpawnedEntities()
    {
        lock (_contextLock)
        {
            return _сontext.SpawnedEntities.ToList();
        }
    }

    public static void Save(List<ModelOnDisk> models)
    {
        lock (_contextLock)
        {
            var oldModels = GetModelsOnDisk();

            _сontext.ModelsOnDisk.RemoveRange(oldModels);
            _сontext.SaveChanges();
            _сontext.ModelsOnDisk.AddRange(models);
            _сontext.SaveChanges();
        }
    }

    public static void Save(List<SpawnedEntityMetadata> spawnedObjects)
    {
        lock (_contextLock)
        {
            var oldObjects = GetSpawnedEntities();

            _сontext.SpawnedEntities.RemoveRange(oldObjects);
            _сontext.SaveChanges();
            _сontext.SpawnedEntities.AddRange(spawnedObjects);
            _сontext.SaveChanges();
        }
    }
}
