using System.Diagnostics;

using CatGen.Utils;

using SharpDX;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D12.Device;
using Resource = SharpDX.Direct3D12.Resource;

namespace CatGen;

/// <summary>
///     Определяет поддиапазон геометрии в MeshGeometry.
///     Используется, когда несколько геометрий хранятся в одном буфере вершин и индексов.
///     Предоставляет смещения и данные, необходимые для отрисовки подмножества геометрии,
///     хранящегося в буферах вершин и индексов
/// </summary>
public class MeshGeometry : IDisposable
{
    private MeshGeometry()
    {
    }

    private readonly List<IDisposable> _toDispose = [];

    /// <summary>
    ///     Имя геометрии
    /// </summary>
    public GeometryEnum Name { get; set; }

    /// <summary>
    ///     Размер вершины в байтах
    /// </summary>
    public int VertexByteStride { get; set; }

    /// <summary>
    ///     Длина буфера вершин в байтах
    /// </summary>
    public int VertexBufferByteSize { get; set; }

    /// <summary>
    ///     Формат индексов
    /// </summary>
    public Format IndexFormat { get; set; }

    /// <summary>
    ///     Размер буфера индексов в байтах
    /// </summary>
    public int IndexBufferByteSize { get; set; }

    /// <summary>
    ///     Количество индексов
    /// </summary>
    public int IndexCount { get; set; }

    /// <summary>
    ///     Буфер вершин
    /// </summary>
    public Resource VertexBufferGPU { get; set; }

    /// <summary>
    ///     Буфер индексов
    /// </summary>
    public Resource IndexBufferGPU { get; set; }

    /// <summary>
    ///     Копия буфера вершин в общей памяти
    /// </summary>
    public object VertexBufferCPU { get; set; }

    /// <summary>
    ///     Копия буфера индексов в общей памяти
    /// </summary>
    public object IndexBufferCPU { get; set; }

    /// <summary>
    ///     Дескриптор на буфер вершин
    /// </summary>
    public VertexBufferView VertexBufferView =>
        new()
        {
            BufferLocation = VertexBufferGPU.GPUVirtualAddress,
            StrideInBytes = VertexByteStride,
            SizeInBytes = VertexBufferByteSize,
        };

    /// <summary>
    ///     Дескриптор на буфер индексов
    /// </summary>
    public IndexBufferView IndexBufferView =>
        new()
        {
            BufferLocation = IndexBufferGPU.GPUVirtualAddress,
            Format = IndexFormat,
            SizeInBytes = IndexBufferByteSize,
        };

    /// <summary>
    ///     Фабрика геометрии. Используется вместо конструктора, чтобы можно было использовать Generic типы как аргументы
    /// </summary>
    /// <param name="device"></param>
    /// <param name="commandList"></param>
    /// <param name="vertices"></param>
    /// <param name="indices"></param>
    /// <param name="name"></param>
    /// <typeparam name="TVertex"></typeparam>
    /// <typeparam name="TIndex"></typeparam>
    /// <returns></returns>
    public static MeshGeometry New<TVertex, TIndex>(
        Device device,
        GraphicsCommandList commandList,
        IEnumerable<TVertex> vertices,
        IEnumerable<TIndex> indices,
        GeometryEnum name)
        where TVertex : struct
        where TIndex : struct
    {
        var vertexArray = vertices.ToArray();
        var indexArray = indices.ToArray();

        var vertexBufferByteSize = Utilities.SizeOf(vertexArray);
        var vertexBuffer = BufferUtil.CreateDefaultBuffer(
            device,
            commandList,
            vertexArray,
            vertexBufferByteSize,
            out var vertexBufferUploader);

        var indexBufferByteSize = Utilities.SizeOf(indexArray);
        var indexBuffer = BufferUtil.CreateDefaultBuffer(
            device, commandList,
            indexArray,
            indexBufferByteSize,
            out var indexBufferUploader);

        return new MeshGeometry
        {
            Name = name,
            VertexByteStride = Utilities.SizeOf<TVertex>(),
            VertexBufferByteSize = vertexBufferByteSize,
            VertexBufferGPU = vertexBuffer,
            VertexBufferCPU = vertexArray,
            IndexCount = indexArray.Length,
            IndexFormat = GetIndexFormat<TIndex>(),
            IndexBufferByteSize = indexBufferByteSize,
            IndexBufferGPU = indexBuffer,
            IndexBufferCPU = indexArray,
            _toDispose =
            {
                vertexBuffer,
                vertexBufferUploader,
                indexBuffer,
                indexBufferUploader,
            },
        };
    }

    private static Format GetIndexFormat<TIndex>()
    {
        var format = Format.Unknown;
        if (typeof(TIndex) == typeof(int))
            format = Format.R32_UInt;
        else if (typeof(TIndex) == typeof(short))
            format = Format.R16_UInt;

        Debug.Assert(format != Format.Unknown);

        return format;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var disposable in _toDispose)
            disposable.Dispose();

        GC.SuppressFinalize(this);
    }
}
