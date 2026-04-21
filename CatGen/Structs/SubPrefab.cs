using SharpDX.Direct3D;

namespace CatGen;

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
