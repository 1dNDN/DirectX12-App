using CatGen.DTOs;

namespace CatGen.Interfaces;

public interface IRenderEngine
{
    void AddModel(ModelOnDisk item);

    void DeleteModel(ModelOnDisk item);

    void SpawnObject(SpawnedObject spawnedObject);

    void DespawnObject(SpawnedObject item);

    void UpdateObject(SpawnedObject item);
}
