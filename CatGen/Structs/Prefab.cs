using SharpDX;

namespace CatGen;

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
