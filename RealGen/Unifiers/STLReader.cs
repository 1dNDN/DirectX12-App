using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using RealGen.Utils;

using SharpDX;

namespace RealGen.Unifiers;

public static class STLReader
{
    public static MeshData Import(string filePath)
    {
        if (!File.Exists(filePath))
            throw new Exception($"Skill issue, file {filePath} not found");

        var vertices = new List<BiggaVertex>();
        var indices = new List<int>();
        var index = 0;

        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
        // Read 80-byte header
        reader.ReadBytes(80);

        // Read the number of triangles (faces)
        var numTriangles = reader.ReadUInt32();

        for (var i = 0; i < numTriangles; i++)
        {
            // Read the normal vector of the triangle (unused)
            reader.ReadBytes(12);

            // Read vertices of the triangle
            for (var j = 0; j < 3; j++)
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();

                var vertex = new BiggaVertex(new Vector3(x, y, z), Vector3.Zero, Vector3.Zero, Vector2.Zero);
                vertices.Add(vertex);
                indices.Add(vertices.Count - 1);
            }

            // Read attribute byte count (unused)
            reader.ReadUInt16();
        }

        var mesh = new MeshData();
        mesh.Vertices.AddRange(vertices);
        mesh.Indices32.AddRange(indices);

        return mesh;
    }
}
