using CatGen.DTOs;
using CatGen.Unifiers;
using CatGen.Utils;

using SharpDX;
using SharpDX.Direct3D12;

#pragma warning disable CA1822
namespace CatGen;

/// <summary>
/// Сервис для управления моделями и сущностями на сцене
/// </summary>
public class SceneResourcesService : IDisposable
{
    /// <summary>
    /// Конструктор сервиса
    /// </summary>
    /// <param name="renderDevice"></param>
    /// <param name="renderCommandList"></param>
    public SceneResourcesService(Device renderDevice, GraphicsCommandList renderCommandList)
    {
        RenderDevice = renderDevice;
        RenderCommandList = renderCommandList;

    }

    //TODO: надо несколько геометрий. Зачем - не помню. См Fence и Semitransparent (blending).

    /// <summary>
    /// Геометрия для сцены
    /// </summary>
    public MeshGeometry Geometry { get; set; } = null!;

    /// <summary>
    /// Материалы для геометрий сцены
    /// </summary>
    public List<Material> Materials { get; } = new();

    /// <summary>
    /// Текстуры для геометрий сцены
    /// </summary>
    public Dictionary<string, Texture> Textures { get; } = new();

    /// <summary>
    /// Список путей используемых 3д моделей
    /// </summary>
    public List<ModelOnDisk> ModelsPaths { get; } = [];

    /// <summary>
    /// Список префабов, готовых для спавна на сцене
    /// </summary>
    public Dictionary<string, Prefab> Prefabs { get; } = [];

    /// <summary>
    /// Список заспавненных сущностей
    /// </summary>
    public List<SpawnedEntityMetadata> EntitiesMetadata { get; } = [];

    /// <summary>
    /// Список всех объектов геометрии сцены
    /// </summary>
    public readonly List<RenderEntity> SceneItems = [];

    /// <summary>
    /// Список всех объектов геометрии сцены, поделенных по PSO
    /// </summary>
    public readonly Dictionary<RenderLayer, List<RenderEntity>> SceneItemLayers = new(1)
    {
        [RenderLayer.Opaque] = [],
    };

    /// <summary>
    /// Адаптер, на котором будем рендерить
    /// </summary>
    public readonly Device RenderDevice;

    /// <summary>
    /// Список команд для GPU
    /// </summary>
    public readonly GraphicsCommandList RenderCommandList;

    /// <summary>
    /// Лок для изменений сцены
    /// </summary>
    public readonly ReaderWriterLockSlim SceneLock = new();

    private bool _dirty = true;

    /// <summary>
    /// Загрузить все модели и объекты
    /// </summary>
    public void Load()
    {
        SceneLock.EnterWriteLock();
        try
        {
            LoadScenes();
            BuildScene();

            _dirty = false;
        }
        finally
        {
            SceneLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Проверяет, надо ли перестраивать буферы. Если надо - делает это.
    /// </summary>
    /// <param name="resetCommandList"></param>
    /// <returns>Есть ли изменения</returns>
    public bool Update(Action resetCommandList)
    {
        SceneLock.EnterWriteLock();
        try
        {
            if (!_dirty)
                return false;

            resetCommandList();


            UpdateScenes();
            UpdateScene();

            _dirty = false;
            return true;
        }
        finally
        {
            SceneLock.ExitWriteLock();
        }
    }

    private void LoadScenes()
    {
        var builder = new MeshGeometryBuilder();

        foreach (var modelPath in ModelsPaths)
        {
            try
            {
                var extension = Path.GetExtension(modelPath.FilePath);

                if (!extension.Equals(".gltf", StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception($"путь {modelPath.FilePath} - говно и не поддерживается");

                var sceneData = GltfReader.Import(modelPath.FilePath, new SceneData(RenderDevice));

                var textureHeapOffset = Textures.Count;

                foreach (var node in sceneData.Nodes)
                {
                    builder.AppendMeshData(node);

                    AddMaterial(node.NodeMaterial, textureHeapOffset);
                }

                for (var i = 0; i < sceneData.Textures.Count; i++)
                {
                    var texture = sceneData.Textures[i];
                    Textures.Add(modelPath.Id + " | " + i, texture);
                }

                var prefab = new Prefab
                {
                    World = sceneData.NormalizedWorld,
                    Textures = sceneData.Textures,
                    SubMeshes = builder.GetAndResetSubmeshes(),
                };

                builder.Track(prefab);

                Prefabs.Add(modelPath.Id, prefab);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{modelPath} - говно ебаное");
                Console.WriteLine(ex);
            }
        }

        Geometry = builder.BuildGeometry(RenderDevice, RenderCommandList);
    }


    /// <summary>
    /// Если список моделей изменился - грузит их заново с диска
    /// </summary>
    private void UpdateScenes()
    {
        Geometry.Dispose();

        foreach (var texture in Textures)
            texture.Value.Dispose();

        Textures.Clear();
        Materials.Clear();


        LoadScenes();
    }

    /// <summary>
    /// Добавляет материал с учётом индексов в буферах
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="textureHeapOffset"></param>
    private void AddMaterial(Material mat, int textureHeapOffset)
    {
        var materialIndex = Materials.Count;

        mat.MaterialCbIndex = materialIndex;
        mat.DiffuseSrvHeapIndex = textureHeapOffset + mat.DiffuseTexture ?? -1;
        Materials.Add(mat);
    }


    /// <summary>
    /// Инициализирует и заполняет все структуры, описывающие сцену для GPU.
    /// Размещает объекты на сцене.
    /// </summary>
    public void BuildScene()
    {
        foreach (var entity in EntitiesMetadata)
        {
            var prefab = Prefabs[entity.ModelOnDiskId];

            var world = CreateWorldMatrix(entity);

            var renderItem = new RenderEntity()
            {
                EntityId = entity.Id,
                ObjCbIndex = SceneItems.Count,
                EntityModel = prefab,
                BaseWorld = world,
            };

            var layer = RenderLayer.Opaque; //TODO: сделать нормально

            if (!SceneItemLayers.ContainsKey(layer))
                SceneItemLayers[layer] = [];

            SceneItemLayers[layer].Add(renderItem);
            SceneItems.Add(renderItem);
        }
    }

    /// <summary>
    /// Если что-то поменялось в составе ресурсов - грузит сцену заново
    /// </summary>
    private void UpdateScene()
    {
        SceneItems.Clear();
        SceneItemLayers.Clear();

        BuildScene();
    }

    /// <summary>
    /// Создаёт матрицу мира для модели
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private static Matrix CreateWorldMatrix(SpawnedEntityMetadata entity)
    {
        return Matrix.Translation(entity.X, entity.Y, entity.Z)
               * Matrix.Scaling(entity.Scale)
               * Matrix.RotationYawPitchRoll(entity.Yaw, entity.Pitch, entity.Roll);
    }

    /// <summary>
    /// Добавляет модели в сцену
    /// </summary>
    /// <param name="models"></param>
    public void AddModels(List<ModelOnDisk> models)
    {
        foreach (var model in models)
        {
            AddModel(model);
        }
    }

    /// <summary>
    /// Добавляет одну модель в сцену
    /// </summary>
    /// <param name="path"></param>
    public void AddModel(ModelOnDisk path)
    {
        SceneLock.EnterWriteLock();
        try
        {
            ModelsPaths.Add(path);
            Dirty();
        }
        finally
        {
            SceneLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Удаляет модель из сцены
    /// </summary>
    /// <param name="item"></param>
    public void DeleteModel(ModelOnDisk item)
    {
        SceneLock.EnterWriteLock();
        try
        {
            var oldItem = ModelsPaths.FirstOrDefault(m => m.Id == item.Id);

            if (oldItem == null)
                return;

            ModelsPaths.Remove(oldItem);

            for (var i = EntitiesMetadata.Count - 1; i >= 0; i--)
            {
                var entity = EntitiesMetadata[i];
                if (entity.ModelOnDiskId == oldItem.Id)
                    EntitiesMetadata.Remove(entity);
            }

            Dirty();
        }
        finally
        {
            SceneLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Спавнит сущности на сцене
    /// </summary>
    /// <param name="entities"></param>
    public void SpawnEntities(List<SpawnedEntityMetadata> entities)
    {
        foreach (var entity in entities)
            SpawnEntity(entity);
    }

    /// <summary>
    /// Спавнит одну сущность на сцене
    /// </summary>
    /// <param name="entity"></param>
    public void SpawnEntity(SpawnedEntityMetadata entity)
    {

        SceneLock.EnterWriteLock();
        try
        {
            this.EntitiesMetadata.Add(entity);
            Dirty();
        }
        finally
        {
            SceneLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Деспавнит сущность из сцены
    /// </summary>
    /// <param name="item"></param>
    public void DespawnEntity(SpawnedEntityMetadata item)
    {
        SceneLock.EnterWriteLock();
        try
        {
            var oldEntity = EntitiesMetadata.FirstOrDefault(e => e.Id == item.Id);

            if (oldEntity == null)
                return;

            EntitiesMetadata.Remove(oldEntity);

            Dirty();
        }
        finally
        {
            SceneLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Обновляет параметры сущности на сцене
    /// </summary>
    /// <param name="item"></param>
    public void EditEntity(SpawnedEntityMetadata item)
    {
        SceneLock.EnterWriteLock();
        try
        {
            var oldItem = EntitiesMetadata.FirstOrDefault(e => e.Id == item.Id);

            // вообще наверное всегда должно быть заспавнено, если есть что обновлять
            if (oldItem == null)
            {
                EntitiesMetadata.Add(item);

                return;
            }

            oldItem.X = item.X;
            oldItem.Y = item.Y;
            oldItem.Z = item.Z;
            oldItem.Pitch = item.Pitch;
            oldItem.Roll = item.Roll;
            oldItem.Yaw = item.Yaw;
            oldItem.Scale = item.Scale;

            var renderItem = SceneItems.FirstOrDefault(e => e.EntityId == item.Id);

            if (renderItem == null)
                return;

            renderItem.BaseWorld = CreateWorldMatrix(oldItem);
        }
        finally
        {
            SceneLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Есть изменения в составе ресурсов. Внимание: сущности за матрицей мира следят самостоятельно!
    /// </summary>
    private void Dirty()
    {
        _dirty = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Geometry.Dispose();

        foreach (var texture in Textures.Values)
            texture.Dispose();

        GC.SuppressFinalize(this);
    }
}
