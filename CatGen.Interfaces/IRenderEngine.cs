using CatGen.DTOs;

namespace CatGen.Interfaces;

public interface IRenderEngine
{
    void AddModel(ModelOnDisk item);

    void DeleteModel(ModelOnDisk item);

    void SpawnObject(SpawnedEntityMetadata spawnedObject);

    void DespawnEntity(SpawnedEntityMetadata item);

    void EditEntity(SpawnedEntityMetadata item);
}
