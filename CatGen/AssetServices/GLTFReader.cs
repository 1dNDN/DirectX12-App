using System;
using System.Diagnostics;
using System.Linq;

using CatGen.Utils;

using SharpDX;

using SharpGLTF.Memory;
using SharpGLTF.Schema2;

using Image = SharpGLTF.Schema2.Image;

// ReSharper disable RedundantNameQualifier


namespace CatGen.Unifiers;

/// <summary>
/// Импортирует модель GLTF
/// </summary>
public class GLTFReader
{
    /// <summary>
    /// Импортирует модель GLTF
    /// </summary>
    public static MeshData ImportGeometry(string filepath)
    {
        var model = ModelRoot.Load(filepath);

        var scene = model.DefaultScene; //TODO: стоит учесть, что может быть 0 сцен и N сцен

        var mesh = new MeshData();

        foreach (var node in scene.VisualChildren)
        {
            LoadNode(mesh, node, Matrix.Identity);
        }


        return mesh;
    }

    //TODO: сделать умный путь, а не эту хуйню
    public static (Image texture, TextureSampler sampler) ImportTexture(string filepath)
    {
        var model = ModelRoot.Load(filepath);

        var logicalTexture = model.LogicalTextures.First();
        var texture = logicalTexture.PrimaryImage;
        var sampler = logicalTexture.Sampler;

        return (texture, sampler);
    }

    private static void LoadNode(MeshData meshData, Node node, Matrix parentTransform)
    {
        var worldTransform = node.LocalMatrix.ToMatrix() * parentTransform;

        if(node.Mesh != null)
            LoadMesh(meshData, node.Mesh, worldTransform);

        foreach (var child in node.VisualChildren)
            LoadNode(meshData, child, worldTransform);
    }

    private static void LoadMesh(MeshData meshData, Mesh nodeMesh, Matrix parentTransform)
    {
        foreach (var primitive in nodeMesh.Primitives)
        {
            if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
                throw new Exception($"Не поддерживается ничо, кроме PrimitiveType.TRIANGLES. А тут {primitive.DrawPrimitiveType}");

            var positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();
            var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var indices = primitive.GetIndexAccessor()?.AsIndicesArray();

            Debug.Assert(indices != null, nameof(indices) + " != null");

            foreach (var index in indices)
            {
                AddVertex(meshData, positions, normals, tangents, texCoords, (int)index, parentTransform);
            }
        }
    }

    private static void AddVertex(MeshData meshData, IAccessorArray<System.Numerics.Vector3> positions, IAccessorArray<System.Numerics.Vector3> normals, IAccessorArray<System.Numerics.Vector4> tangents, IAccessorArray<System.Numerics.Vector2> texCoords, int index, Matrix parentTransform)
    {
        var position = SharpDX.Vector3
            .Transform(positions[index].ToVector3SharpDX(), parentTransform)
            .ToVector3SharpDX();

        SharpDX.Vector3 normal;
        if (normals == null)
        {
            normal = SharpDX.Vector3.UnitY;
        }
        else
        {
            normal = SharpDX.Vector3.TransformNormal(normals[index].ToVector3SharpDX(), parentTransform);
            normal.Normalize();
        }

        SharpDX.Vector3 tangent;

        if (tangents == null)
        {
            tangent = SharpDX.Vector3.UnitX;
        }
        else
        {
            tangent = SharpDX.Vector3.TransformNormal(new SharpDX.Vector3(tangents[index].X, tangents[index].Y, tangents[index].Z), parentTransform);
            tangent.Normalize();
        }

        SharpDX.Vector2 textureCoordinate;

        if (texCoords == null)
        {
            textureCoordinate = SharpDX.Vector2.Zero;
        }
        else
        {
            textureCoordinate = texCoords[index].ToVector2SharpDX();
        }

        var vertex = new BiggaVertex(position, normal, tangent, textureCoordinate);
        meshData.Vertices.Add(vertex);
        meshData.Indices.Add(meshData.Vertices.Count - 1);
    }
}
