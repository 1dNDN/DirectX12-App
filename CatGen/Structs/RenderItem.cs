using SharpDX;
using SharpDX.Direct3D;
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace CatGen;

/// <summary>
/// Класс конкретного объекта в мире - тот экземпляр, который конкретно рендерится
/// </summary>
public class RenderEntity : Dirtyable
{
    /// <summary>
    /// ID энтити, которое рендерим
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Модель этого объекта с мешами, текстурами и материалами
    /// </summary>
    public Prefab EntityModel { get; set; }

    private Matrix? _world;
    private Matrix _baseWorld = Matrix.Identity;

    /// <summary>
    /// Матрица, описывающая сущность относительно мира. Произведение матрицы мира сабмеша на матрицу мира сущности
    /// </summary>
    public Matrix World => _world ??= BaseWorld;

    /// <summary>
    /// Матрица, описывающая саму сущность относительно мира. В частности, позицию, направление и масштаб
    /// </summary>
    public Matrix BaseWorld
    {
        get {
            return _baseWorld;
        }
        set {
            _world = null;
            _baseWorld = value;
            Dirty();
        }
    }

    /// <summary>
    /// Индекс объекта в буфере констант для этого объекта
    /// </summary>
    public int ObjCbIndex { get; set; } = -1;

    public Matrix TexTransform { get; set; } = Matrix.Identity;
}

/// <summary>
/// Класс шаблона объекта, который можно отрендерить. Предназначен для того, чтобы ссылаться на него в <see cref="RenderEntity"/>.
/// Содержит в себе от 1 до N мешей со своими текстурами и всем таким.
/// </summary>
public class Prefab : Dirtyable
{
    /// <summary>
    /// Матрица мира, общая для всех подгеометрий
    /// </summary>
    public Matrix World { get; set; } = Matrix.Identity;

    /// <summary>
    /// Геометрия, в которой лежат меши шаблона объекта
    /// </summary>
    public MeshGeometry Geo { get; set; }

    /// <summary>
    /// Список мешей объекта
    /// </summary>
    public List<SubPrefab> SubMeshes { get; set; } = [];

    /// <summary>
    /// Список текстур объекта
    /// </summary>
    public List<Texture> Textures { get; set; } = [];
}

/// <summary>
/// Класс конкретного меша в составе объекта, который мы собираемся отрендерить. Предназначен для того, чтобы хранить конкретные данные об атомарном куске меша.
/// Содержит в себе 1 меш с текстурами и материалом.
/// Зависит от <see cref="Prefab.Geo"/> - индексы в SubPrefab ссылаются на Geo в Prefab.
/// </summary>
public class SubPrefab : Dirtyable
{
    /// <summary>
    /// Материал меша
    /// </summary>
    public Material Mat { get; set; }

    /// <summary>
    /// Тип топологии меша
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

/// <summary>
/// Абстрактный класс для всех сущностей, которые можно сделать Dirty
/// </summary>
public abstract class Dirtyable
{
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
    /// Объект изменился, теперь он дёрти и надо грузить снова
    /// </summary>
    public void Dirty()
    {
        NumFramesDirty = BaseDirectXWindow.NumFrameResources;
    }
}
