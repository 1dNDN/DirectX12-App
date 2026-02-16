using ObjParser;

using CatGen.Utils;

namespace CatGen.Unifiers;

/// <summary>
/// Класс для импорта моделей в формате OBJ/MTL
/// </summary>
public static class ObjReader
{
    /// <summary>
    /// Импортирует только геометрию
    /// </summary>
    /// <param name="objPath">Путь до объекта</param>
    /// <returns>Геометрия объекта</returns>
    public static MeshData Import(string objPath)
    {
        var obj = new ObjModel();
        obj.Load(objPath);

        var mesh = new MeshData();

        for (var i = 0; i < obj.Vertices.Count; i++)
        {
            var vertex = obj.Vertices[i];
            var normal = obj.Normals[i];

            var bigga = new BiggaVertex(
                (float)vertex.X, (float)vertex.Y, (float)vertex.Z,
                (float)normal.K, (float)normal.I, (float)normal.J,
                0f,0f,0f,0f,0f
            );

            mesh.Vertices.Add(bigga);

        }

        foreach (var face in obj.Faces)
        foreach (var index in face.VertexIndexList)
            mesh.Indices.Add(index - 1);

        return mesh;
    }
}
