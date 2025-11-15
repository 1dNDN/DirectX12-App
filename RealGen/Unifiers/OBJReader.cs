using System;
using System.Linq;

using ObjParser;

using RealGen.Utils;

using SharpDX;

namespace RealGen.Unifiers;

public class OBJReader
{
    public static MeshData Import(string filePath)
    {
        var obj = new ObjModel();
        obj.Load(filePath);


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
            mesh.Indices32.Add(index - 1);

        return mesh;
    }
}
