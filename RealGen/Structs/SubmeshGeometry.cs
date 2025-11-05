namespace RealGen;

/// <summary>
/// Определяет часть геометрии в MeshGeometry. Используется, когда несколько геометрий хранятся в одном вертексном и индексном буферах.
/// Предоставляет офсеты, необходимые для выделения подгеометрии из буферов.
/// </summary>
public class SubmeshGeometry
{
    //TODO:
    public int IndexCount { get; set; }
    public int StartIndexLocation { get; set; }
    public int BaseVertexLocation { get; set; }

}
