using System.Diagnostics;

using CatGen.Utils;

using SharpDX;

using SharpGLTF.Memory;
using SharpGLTF.Schema2;

// ReSharper disable RedundantNameQualifier


namespace CatGen.Unifiers;

/// <summary>
/// Импортирует модель GLTF
/// </summary>
public static class GltfReader
{
    private static readonly Dictionary<string, ModelRoot> _modelCache = new();

    /// <summary>
    /// В dx12 координата z смотрит от нас, а в gltf к нам.
    /// </summary>
    private static readonly Matrix _gltf2dxCoordsTransform = Matrix.Scaling(1, 1, -1);

    public static SceneData Import(string filepath, SceneData sceneData)
    {
        var model = Load(filepath);
        var defaultScene = model.DefaultScene;

        foreach (var node in defaultScene.VisualChildren)
        {
            LoadNode(node, _gltf2dxCoordsTransform, ref sceneData);
        }

        foreach (var texture in model.LogicalTextures)
            sceneData.Add(texture);

        return sceneData;
    }

    private static ModelRoot Load(string filepath)
    {
        if (_modelCache.TryGetValue(filepath, out var value))
            return value;

        var modelRoot = ModelRoot.Load(filepath);
        _modelCache.Add(filepath, modelRoot);
        return modelRoot;
    }

    private static void LoadNode(Node node, Matrix parentTransform, ref SceneData scene)
    {
        var worldTransform = node.LocalMatrix.ToMatrix() * parentTransform;

        if(node.Mesh != null)
            LoadMesh(node.Mesh, worldTransform, ref scene);

        foreach (var child in node.VisualChildren)
            LoadNode(child, worldTransform, ref scene);
    }

    private static void LoadMesh(Mesh nodeMesh, Matrix parentTransform, ref SceneData scene)
    {
        foreach (var primitive in nodeMesh.Primitives)
        {
            var meshData = new NodeData();

            if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
                throw new Exception($"Не поддерживается ничо, кроме PrimitiveType.TRIANGLES. А тут {primitive.DrawPrimitiveType}");

            var positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();
            var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var indices = primitive.GetIndexAccessor()?.AsIndicesArray();

            Debug.Assert(indices != null, nameof(indices) + " == null");

            foreach (var index in indices)
            {
                AddVertex(positions, normals, tangents, texCoords, (int)index, parentTransform, ref meshData);
            }

            meshData.NodeMaterial = LoadMaterial(primitive);

            scene.Add(meshData);
        }
    }

    private static void AddVertex(IAccessorArray<System.Numerics.Vector3> positions,
        IAccessorArray<System.Numerics.Vector3>? normals,
        IAccessorArray<System.Numerics.Vector4>? tangents,
        IAccessorArray<System.Numerics.Vector2>? texCoords,
        int index,
        Matrix parentTransform,
        ref NodeData meshData)
    {
        var position = SharpDX.Vector3
            .Transform(positions[index].ToVector3SharpDx(), parentTransform)
            .ToVector3SharpDx();

        SharpDX.Vector3 normal;
        if (normals == null)
        {
            normal = SharpDX.Vector3.UnitY;
        }
        else
        {
            normal = SharpDX.Vector3.TransformNormal(normals[index].ToVector3SharpDx(), parentTransform);
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
            textureCoordinate = texCoords[index].ToVector2SharpDx();
        }

        var vertex = new BiggaVertex(position, normal, tangent, textureCoordinate);
        meshData.Vertices.Add(vertex);
        meshData.Indices.Add(meshData.Vertices.Count - 1);
    }

    private static Material LoadMaterial(MeshPrimitive primitive)
    {
        var logicalMaterial = primitive.Material;

        const string baseColorName = "BaseColor";
        const string metallicRoughnessName = "MetallicRoughness";
        const string metallicfactorName = "MetallicFactor";
        const string roughnessFactorName = "RoughnessFactor";
        const string normalName = "Normal";
        const string normalScaleName = "NormalScale";
        const string occlusionName = "Occlusion";
        const string occlusionStrengthName = "OcclusionStrength";
        const string emissiveName = "Emissive";
        const string emissiveStrengthName = "EmissiveStrength";


        var diffuseAlbedo = logicalMaterial.GetMaterialColor(baseColorName, 1.0f);

        var fresnelR0 = logicalMaterial.GetMaterialFactor(metallicRoughnessName, metallicfactorName, 0.04f);
        var roughness = logicalMaterial.GetMaterialFactor(metallicRoughnessName, roughnessFactorName, 1.0f);
        var normalScale = logicalMaterial.GetMaterialFactor(normalName, normalScaleName, 1.0f);
        var occlusionStrength = logicalMaterial.GetMaterialFactor(occlusionName, occlusionStrengthName, 1.0f);

        var emissiveColor = logicalMaterial.GetMaterialColor(emissiveName, 0.0f);
        var emissiveStrength = logicalMaterial.GetMaterialFactor(emissiveName, emissiveStrengthName, 1.0f);

        var baseTexture = logicalMaterial.GetTextureIndex(baseColorName);
        var metallicRougnessTexture = logicalMaterial.GetTextureIndex(metallicRoughnessName);
        var normalTexture = logicalMaterial.GetTextureIndex(normalName);
        var occlusionTexture = logicalMaterial.GetTextureIndex(occlusionName);
        var emissiveTexture = logicalMaterial.GetTextureIndex(emissiveName);

        return new Material(
            diffuseAlbedo,
            fresnelR0,
            roughness,
            normalScale,
            occlusionStrength,
            emissiveColor,
            emissiveStrength,
            baseTexture,
            metallicRougnessTexture,
            normalTexture,
            occlusionTexture,
            emissiveTexture);
    }

    private static int? GetTextureIndex(this SharpGLTF.Schema2.Material logicalMaterial, string baseColorName)
    {
        return logicalMaterial.FindChannel(baseColorName)?.Texture?.PrimaryImage?.LogicalIndex;
    }

    private static float GetMaterialFactor(this SharpGLTF.Schema2.Material logicalMaterial, string channelKey, string parameterKey, float defaultValue)
    {
        return (float)(GetMaterialParameter(logicalMaterial, channelKey, parameterKey) ?? defaultValue);
    }

    private static SharpDX.Vector4 GetMaterialColor(this SharpGLTF.Schema2.Material logicalMaterial, string channelKey, float defaultValue)
    {
        var rgba = GetMaterialParameter(logicalMaterial, channelKey, "RGBA");

        if (rgba != null)
            return ((System.Numerics.Vector4)rgba).ToVector4Numerics();

        var rgb = GetMaterialParameter(logicalMaterial, channelKey, "RGB");

        if(rgb != null)
            return new SharpDX.Vector4(((System.Numerics.Vector3)rgb).ToVector3SharpDx(), 1.0F);

        return new SharpDX.Vector4(defaultValue);
    }

    private static object? GetMaterialParameter(this SharpGLTF.Schema2.Material logicalMaterial, string channelKey, string parameterKey) =>
        logicalMaterial
            .FindChannel(channelKey)
            ?.Parameters
            .FirstOrDefault(p => string.Equals(p.Name, parameterKey, StringComparison.InvariantCultureIgnoreCase))
            ?.Value;
}
