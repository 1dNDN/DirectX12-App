using SharpDX;

namespace CatGen;

/// <summary>
/// Определяет часть геометрии в MeshGeometry. Используется, когда несколько геометрий хранятся в одном вертексном и индексном буферах.
/// Предоставляет офсеты, необходимые для выделения подгеометрии из буферов.
/// </summary>
public class SubmeshGeometry
{
    /// <summary>
    /// Количество индексов подгеометрии
    /// </summary>
    public int IndexCount { get; set; }

    /// <summary>
    /// Место, где начинаются индексы подгеометрии в буфере
    /// </summary>
    public int StartIndexLocation { get; set; }

    /// <summary>
    /// Место, где начинаются вертексы геометрии в буфере
    /// </summary>
    public int BaseVertexLocation { get; set; }

    /// <summary>
    /// Матрица мира подгеометрии
    /// </summary>
    public Matrix World { get; set; } = Matrix.Identity;

}
