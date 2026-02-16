using CatGen.DTOs;
using CatGen.Unifiers;
using CatGen.Utils;

using SharpDX;
using SharpDX.Direct3D12;

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
    public Dictionary<string, Material> Materials { get; set; } = new();

    /// <summary>
    /// Текстуры для геометрий сцены
    /// </summary>
    public Dictionary<string, Texture> Textures { get; set; } = new();

    /// <summary>
    /// Список путей используемых 3д моделей
    /// </summary>
    public List<ModelOnDisk> ModelsPaths { get; set; } = [];

    /// <summary>
    /// Список заспавненных сущностей
    /// </summary>
    public List<SpawnedEntityMetadata> EntitiesMetadata { get; set; } = [];

    /// <summary>
    /// Список всех объектов геометрии сцены
    /// </summary>
    public readonly List<RenderItem> SceneItems = [];

    /// <summary>
    /// Список всех объектов геометрии сцены, поделенных по PSO
    /// </summary>
    public readonly Dictionary<RenderLayer, List<RenderItem>> SceneItemLayers = new(1)
    {
        [RenderLayer.Opaque] = [],
    };

    /// <summary>
    /// Адаптер, на котором будем рендерить
    /// </summary>
    public Device RenderDevice;

    /// <summary>
    /// Список команд для GPU
    /// </summary>
    public GraphicsCommandList RenderCommandList;

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
            LoadModels();
            LoadMaterials();
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

            UpdateModels();
            UpdateMaterials();
            UpdateScene();

            _dirty = false;
            return true;
        }
        finally
        {
            SceneLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Грузит модели с диска в память
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void LoadModels()
    {
        var vertices = new List<BiggaVertex>();
        var indices = new List<int>();
        var geometries = new List<(string, SubmeshGeometry)>();

        foreach (var modelPath in ModelsPaths)
        {
            try
            {
                var extention = Path.GetExtension(modelPath.FilePath);

                MeshData? mesh = null;
                if (extention.Equals(".gltf", StringComparison.InvariantCultureIgnoreCase))
                {
                    mesh = GltfReader.ImportGeometry(modelPath.FilePath);
                }
                else if (extention.Equals(".obj", StringComparison.InvariantCultureIgnoreCase))
                {
                    mesh = ObjReader.Import(modelPath.FilePath);
                }
                else if (extention.Equals(".stl", StringComparison.InvariantCultureIgnoreCase))
                {
                    mesh = StlReader.Import(modelPath.FilePath);
                }

                if (mesh == null)
                    throw new Exception($"путь {modelPath.FilePath} - говно и не поддерживается");

                var submesh = GeometryGenerator.AppendMeshData(mesh, vertices, indices);
                geometries.Add(new ValueTuple<string, SubmeshGeometry>(modelPath.Id, submesh));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{modelPath} - говно ебаное");
                Console.WriteLine(ex);
            }
        }

        var geo = MeshGeometry.New(RenderDevice, RenderCommandList, vertices, indices.ToArray(), GeometryEnum.Master);

        foreach (var submeshGeometry in geometries)
            geo.DrawArgs.Add(submeshGeometry.Item1, submeshGeometry.Item2);
        // TODO: Хуйня какая-то получилась. Переделать надо

        Geometry = geo;
    }

    /// <summary>
    /// Если список моделей изменился - грузит их заново с диска
    /// </summary>
    private void UpdateModels()
    {
        Geometry.Dispose();

        LoadModels();
    }

    /// <summary>
    /// Грузит материалы и текстуры
    /// </summary>
    public void LoadMaterials()
    {
        //TODO: добавить поддержку не только GLTF

        foreach (var modelPath in ModelsPaths)
        {
            var extention = Path.GetExtension(modelPath.FilePath);

            if (extention.Equals(".gltf", StringComparison.InvariantCultureIgnoreCase))
            {
                var (img, sampler) = GltfReader.ImportTexture(modelPath.FilePath);

                var texture = new Texture
                {
                    Resource = TextureUtil.CreateTextureFromPng(RenderDevice, img),
                };

                Textures.Add(modelPath.Id, texture);

                var material = GltfReader.ImportMaterial(modelPath.FilePath);
                material.NameStr = modelPath.Id;

                AddMaterial(material);
            }
        }
    }

    /// <summary>
    /// Если материалы изменились - грузит заново
    /// </summary>
    private void UpdateMaterials()
    {
        foreach (var texture in Textures)
            texture.Value.Dispose();

        Textures.Clear();
        Materials.Clear();

        LoadMaterials();
    }

    /// <summary>
    /// Добавляет материал с учётом индексов в буферах
    /// </summary>
    /// <param name="mat"></param>
    private void AddMaterial(Material mat)
    {
        var materialIndex = Materials.Count;

        mat.MaterialCbIndex = materialIndex;
        mat.DiffuseSrvHeapIndex = materialIndex;
        Materials.Add(mat.NameStr, mat);
    }


    /// <summary>
    /// Инициализирует и заполняет все структуры, описывающие сцену для GPU.
    /// Размещает объекты на сцене.
    /// </summary>
    public void BuildScene()
    {
        foreach (var entity in EntitiesMetadata)
        {
            var submesh = Geometry.DrawArgs[entity.ModelOnDiskId];

            var world = CreateWorldMatrix(entity);

            var renderItem = new RenderItem
            {
                EntityId = entity.Id,
                ObjCbIndex = SceneItems.Count,
                Geo = Geometry,
                IndexCount = submesh.IndexCount,
                StartIndexLocation = submesh.StartIndexLocation,
                BaseVertexLocation = submesh.BaseVertexLocation,
                SubmeshWorld = submesh.World,
                BaseWorld = world,
                Mat = Materials[entity.ModelOnDiskId],
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
    }
}
