using SharpDX;
using SharpDX.Direct3D12;

namespace CatGen.Utils;

/// <summary>
/// Данные о геометрии независимого объекта
/// </summary>
public class NodeData
{
    /// <summary>
    /// Вершины геометрии объекта
    /// </summary>
    public List<BiggaVertex> Vertices { get; } = [];

    /// <summary>
    /// Индексы геометрии объекта
    /// </summary>
    public List<int> Indices { get; } = [];

    public Material NodeMaterial { get; set; } = null!;
}


public class SceneData
{
    public SceneData(Device device)
    {
        RenderDevice = device;
    }

    public Device RenderDevice { get; set; }

    private List<BiggaVertex>? _vertices = null;
    private List<int>? _indices = null;

    /// <summary>
    /// Вершины геометрии объекта
    /// </summary>
    public List<BiggaVertex> Vertices => _vertices ??= Nodes.SelectMany(node => node.Vertices).ToList();

    /// <summary>
    /// Индексы геометрии объекта
    /// </summary>
    public List<int> Indices => _indices ??= Nodes.SelectMany(node => node.Indices).ToList();

    /// <summary>
    /// Координата средней точки объекта
    /// </summary>
    public Vector3 AvgLocation =>
        new(
            Vertices.Select(vertex => vertex.Position.X).Average(),
            Vertices.Select(vertex => vertex.Position.Y).Average(),
            Vertices.Select(vertex => vertex.Position.Z).Average()
        );

    /// <summary>
    /// Размер объекта по крайним точкам
    /// </summary>
    public float Size =>
        Vertices.Select(vertex =>
                Math.Max(
                    Math.Max(
                        Math.Abs(vertex.Position.X),
                        Math.Abs(vertex.Position.Y)),
                    Math.Abs(vertex.Position.Z)))
            .Max();

    /// <summary>
    /// Матрица нормализованного мира объекта. Центрирует объект в 0,0,0 и выставляет масштаб так, что размер 1.
    /// </summary>
    public Matrix NormalizedWorld => Matrix.Translation(-AvgLocation) * Matrix.Scaling(5 / Size);

    public List<NodeData> Nodes { get; } = [];

    public List<Texture> Textures { get; set; } = [];

    public void Add(NodeData node)
    {
        _vertices = null;
        _indices = null;

        Nodes.Add(node);
    }

    public void Add(SharpGLTF.Schema2.Texture texture)
    {
        var value = new Texture
        {
            Resource = TextureUtil.CreateTextureFromPng(RenderDevice, texture.PrimaryImage),
        };

        Textures.Add(value);
    }
}
