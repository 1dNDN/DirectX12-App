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

        mesh.Vertices.AddRange(obj.Vertices.Select(vertex => new BiggaVertex((float)vertex.X, (float)vertex.Y, (float)vertex.Z, 0f,0f,0f,0f,0f,0f,0f,0f)));

        foreach (var face in obj.Faces)
        foreach (var index in face.VertexIndexList)
            mesh.Indices32.Add(index - 1);

        return mesh;
    }
}
