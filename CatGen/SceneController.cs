using CatGen.DTOs;
using CatGen.Unifiers;
using CatGen.Utils;

namespace CatGen;

public class SceneController
{
    public Dictionary<string, MeshData> Meshes { get; set; }

    public List<ModelOnDisk> ModelsPaths { get; set; }

    public List<SpawnedObjectMetadata> ObjectsMetadata { get; set; }

    public void LoadModels()
    {
        foreach (var modelPath in ModelsPaths)
        {
            var extention = Path.GetExtension(modelPath.FilePath);

            MeshData? mesh = null;
            if (extention.Equals(".gltf", StringComparison.InvariantCultureIgnoreCase))
            {
                mesh = GLTFReader.ImportGeometry(modelPath.FilePath);
            } else if (extention.Equals(".obj", StringComparison.InvariantCultureIgnoreCase))
            {
                mesh = OBJReader.Import(modelPath.FilePath);
            } else if (extention.Equals(".stl", StringComparison.InvariantCultureIgnoreCase))
            {
                mesh = STLReader.Import(modelPath.FilePath);
            }

            if (mesh == null)
                throw new Exception($"путь {modelPath.FilePath} - говно и не поддерживается");

            Meshes.Add(modelPath.Id, mesh);
        }
    }

    public void LoadScene()
    {

    }

    public void AddModel(ModelOnDisk path)
    {

    }

    public void DeleteModel(ModelOnDisk item)
    {}

    public void SpawnObject(SpawnedObjectMetadata spawnedObject)
    {
    }

    public void DespawnObject(SpawnedObjectMetadata item)
    {
    }

    public void UpdateObject(SpawnedObjectMetadata item)
    {
    }
}
