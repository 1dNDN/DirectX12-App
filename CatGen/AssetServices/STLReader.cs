using CatGen.Utils;

using SharpDX;

namespace CatGen.Unifiers;

/// <summary>
/// Класс для импорта моделей в формате STL
/// </summary>
public static class StlReader
{
    /// <summary>
    /// Импортирует только геометрию
    /// </summary>
    /// <param name="filePath">Путь до модели</param>
    /// <returns>Геометрия объекта</returns>
    public static NodeData Import(string filePath)
    {
        if (!File.Exists(filePath))
            throw new Exception($"Skill issue, file {filePath} not found");

        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
        // Read 80-byte header
        reader.ReadBytes(80);

        // Read the number of triangles (faces)
        var numTriangles = reader.ReadUInt32();
        var vertices = new List<BiggaVertex>((int)numTriangles * 3);
        var indices = new List<int>((int)numTriangles * 3);

        for (var i = 0; i < numTriangles; i++)
        {
            var nx = reader.ReadSingle();
            var ny = reader.ReadSingle();
            var nz = reader.ReadSingle();

            // Read vertices of the triangle
            for (var j = 0; j < 3; j++)
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();

                var vertex = new BiggaVertex(new Vector3(x, y, z), new Vector3(nx, ny, nz), Vector3.Zero, Vector2.Zero);
                vertices.Add(vertex);
                indices.Add(vertices.Count - 1);
            }

            // Read attribute byte count (unused)
            reader.ReadUInt16();
        }

        var mesh = new NodeData();
        mesh.Vertices.AddRange(vertices);
        mesh.Indices.AddRange(indices);

        return mesh;
    }
}
