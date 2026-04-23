using CatGen.Utils;

using SharpDX.Direct3D12;

namespace CatGen;

/// <summary>
///     Класс для того, чтобы собрать MeshGeometry
/// </summary>
public class MeshGeometryBuilder
{
    private readonly List<BiggaVertex> _vertices = [];
    private readonly List<int> _indices = [];
    private List<SubPrefab> _subPrefabs = [];
    private readonly List<Prefab> _prefabs = [];

    /// <summary>
    ///     Добавляет меш и возвращает SubPrefab со ссылками на это место в массиве.
    /// </summary>
    /// <param name="meshData"></param>
    /// <returns></returns>
    public void AppendMeshData(NodeData meshData)
    {
        // Определяем SubPrefab которая описывает часть буфера вершин/индексов, содержащую подгеометрию

        var submesh = new SubPrefab
        {
            IndexCount = meshData.Indices.Count,
            BaseVertexLocation = _vertices.Count,
            StartIndexLocation = _indices.Count,
            Material = meshData.NodeMaterial,
        };

        _vertices.AddRange(meshData.Vertices);
        _indices.AddRange(meshData.Indices);

        _subPrefabs.Add(submesh);
    }

    /// <summary>
    ///     Сбрасывает список подмешей и возвращает последнее его состояние (для префаба).
    /// </summary>
    /// <returns></returns>
    public List<SubPrefab> GetAndResetSubmeshes()
    {
        var last = _subPrefabs;

        _subPrefabs = [];

        return last;
    }

    /// <summary>
    /// Добавляет префаб в список для отслеживания, чтобы потом проставить Geometry в нём.
    /// </summary>
    /// <param name="prefab"></param>
    public void Track(Prefab prefab)
    {
        _prefabs.Add(prefab);
    }

    /// <summary>
    /// Создаёт новую геометрию и поставляет её во всех отслеживаемых префабах
    /// </summary>
    /// <param name="renderDevice"></param>
    /// <param name="renderCommandList"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public MeshGeometry BuildGeometry(Device renderDevice, GraphicsCommandList renderCommandList)
    {
        var geo = MeshGeometry.New(renderDevice, renderCommandList, _vertices, _indices, GeometryEnum.Master);

        foreach (var prefab in _prefabs)
        {
            prefab.Geo = geo;
        }

        return geo;
    }
}
