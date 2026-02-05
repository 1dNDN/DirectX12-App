using SharpDX;

namespace CatGen;

/// <summary>
/// Класс материала для передачи в шейдер
/// </summary>
public struct MaterialConstants
{
    /// <summary>
    /// Коэффициент отражаемости поверхности
    /// </summary>
    public Vector4 DiffuseAlbedo;

    /// <summary>
    /// Коэффициент эффекта Френеля для нормали
    /// </summary>
    public Vector3 FresnelR0;

    /// <summary>
    /// Шероховатость поверхности
    /// </summary>
    public float Roughness;

    // Used in texture mapping.
    public Matrix MatTransform;

    /// <summary>
    /// Материал по умолчанию
    /// </summary>
    public static MaterialConstants Default => new MaterialConstants
    {
        DiffuseAlbedo = Vector4.One,
        FresnelR0 = new Vector3(0.01f),
        Roughness = 0.25f,
        MatTransform = Matrix.Identity
    };
}
