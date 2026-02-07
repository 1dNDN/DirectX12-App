using CatGen.DTOs;

namespace CatGen.Interfaces;

public interface IRenderEngine
{
    void AddModel(ModelOnDisk item);

    void DeleteModel(ModelOnDisk item);

    void SpawnObject(SpawnedObjectMetadata spawnedObject);

    void DespawnObject(SpawnedObjectMetadata item);

    void UpdateObject(SpawnedObjectMetadata item);
}
