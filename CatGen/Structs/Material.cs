using SharpDX;

using SharpGLTF.Schema2;

namespace CatGen;

/// <summary>
/// Материал поверхности, описывающий отражаемость света от неё
/// </summary>
public class Material : Dirtyable
{
    public Material(Vector4 diffuseAlbedo,
        float fresnelR0,
        float roughness,
        float normalScale,
        float occlusionStrength,
        Vector4 emissiveColor,
        float emissiveStrength,
        AlphaMode alphaMode,
        float alphaCutoff,
        int? baseTexture,
        int? metallicRougnessTexture,
        int? normalTexture,
        int? occlusionTexture,
        int? emissiveTexture)
    {
        DiffuseAlbedo = diffuseAlbedo;

        // не бывает материалов с R0 меньше 0.04
        if (fresnelR0 < 0.04f)
            fresnelR0 = 0.04f;

        FresnelR0 = new Vector3(fresnelR0); //TODO: переделать шейдер для float
        Roughness = roughness;
        NormalScale = normalScale;
        OcclusionStrength = occlusionStrength;
        EmissiveColor = emissiveColor;
        EmissiveStrength = emissiveStrength;
        AlphaMode = alphaMode;
        AlphaCutoff = alphaCutoff;
        DiffuseTexture = baseTexture;
        RoughnessTexture = metallicRougnessTexture;
        NormalTexture = normalTexture;
        OcclusionTexture = occlusionTexture;
        EmissiveTexture = emissiveTexture;

        if (roughness > 0.99F)
            Roughness = 0.99F;
    }

    /// <summary>
    /// Уникальное имя материала для поиска
    /// </summary>
    public string NameStr { get; set; }

    /// <summary>
    /// Индекс константного буфера для материала
    /// </summary>
    public int MaterialCbIndex { get; set; } = -1;

    /// <summary>
    /// Индекс кучи ресурса шейдеров для текстуры диффузного освещения
    /// </summary>
    public int DiffuseSrvHeapIndex { get; set; } = -1;


    /// <summary>
    /// Индекс кучи ресурса шейдеров для текстуры карты нормалей
    /// </summary>
    public int NormalSrvHeapIndex { get; set; } = -1;

    /// <summary>
    /// Коэффициент отражаемости поверхности
    /// </summary>
    public Vector4 DiffuseAlbedo { get; set; }

    /// <summary>
    /// Коэффициент эффекта Френеля для нормали
    /// </summary>
    public Vector3 FresnelR0 { get; set; }

    /// <summary>
    /// Шероховатость поверхности
    /// </summary>
    public float Roughness { get; set; }

    /// <summary>
    /// Масштаб нормалей
    /// </summary>
    public float NormalScale { get; set; }

    /// <summary>5
    /// Базовая текстура поверхности
    /// </summary>
    public int? DiffuseTexture { get; set; }

    /// <summary>
    /// Текстура шероховатости-металлическости поверхности
    /// </summary>
    public int? RoughnessTexture { get; set; }

    /// <summary>
    /// Текстура нормалей
    /// </summary>
    public int? NormalTexture { get; set; }

    /// <summary>
    /// Текстура затенения
    /// </summary>
    public int? OcclusionTexture { get; set; }

    /// <summary>
    /// Текстура свечения
    /// </summary>
    public int? EmissiveTexture { get; set; }

    public float EmissiveStrength { get; set; }

    public Vector4 EmissiveColor { get; set; }

    public float OcclusionStrength { get; set; }

    /// <summary>
    /// Режим работы blending
    /// </summary>
    public AlphaMode AlphaMode;

    /// <summary>
    /// Начиная с какого значения Alpha пиксель можно отбросить
    /// </summary>
    public float AlphaCutoff;

    public Matrix MatTransform { get; set; } = Matrix.Identity;
}
