using SharpDX;
using SharpDX.Direct3D;

namespace CatGen;

/// <summary>
/// Класс объекта, описывающего один объект для рендеринга
/// </summary>
public class RenderItem
{
    /// <summary>
    /// Матрица, описывающая объект относительно мира. В частности, позицию, направление и масштаб
    /// </summary>
    public Matrix World { get; set; } = Matrix.Identity;

    public Matrix TexTransform { get; set; } = Matrix.Identity;

    // Dirty flag indicating the object data has changed and we need to update the constant buffer.
    // Because we have an object cbuffer for each FrameResource, we have to apply the
    // update to each FrameResource. Thus, when we modify obect data we should set
    // NumFramesDirty = gNumFrameResources so that each frame resource gets the update.

    /// <summary>
    /// Флаг, показывающий, что данные объекта изменились и нужно обновить буфер констант в каждом кадре.
    /// Таким образом, <c>NumFramesDirty = BaseDirectXWindow.NumFrameResources</c> при любом изменении.
    /// </summary>
    public int NumFramesDirty { get; set; } = BaseDirectXWindow.NumFrameResources;

    /// <summary>
    /// Индекс объекта в буфере констант для этого объекта
    /// </summary>
    public int ObjCbIndex { get; set; } = -1;

    /// <summary>
    /// Материал объекта
    /// </summary>
    public Material Mat { get; set; }

    /// <summary>
    /// Геометрия объекта
    /// </summary>
    public MeshGeometry Geo { get; set; }

    /// <summary>
    /// Тип топологии объекта
    /// </summary>
    public PrimitiveTopology PrimitiveType { get; set; } = PrimitiveTopology.TriangleList;

    /// <summary>
    /// Количество индексов геометрии
    /// </summary>
    public int IndexCount { get; set; }

    /// <summary>
    /// Место, где начинаются индексы геометрии в буфере
    /// </summary>
    public int StartIndexLocation { get; set; }

    /// <summary>
    /// Место, где начинаются вертексы геометрии в буфере
    /// </summary>
    public int BaseVertexLocation { get; set; }
}
