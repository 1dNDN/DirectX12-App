using System.Runtime.InteropServices;

using SharpDX;

using SharpGLTF.Schema2;

namespace CatGen;

/// <summary>
/// Класс материала для передачи в шейдер
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
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

    /// <summary>
    /// Режим работы blending
    /// </summary>
    public AlphaMode AlphaMode;

    /// <summary>
    /// Начиная с какого значения Alpha пиксель можно отбросить
    /// </summary>
    public float AlphaCutoff;

    /// Used in texture mapping.
    public Matrix MatTransform;

    /// <summary>
    /// Индекс диффузной текстуры
    /// </summary>
    public int DiffuseMapIndex;

    /// <summary>
    /// Индекс карты нормалей
    /// </summary>
    public int NormalMapIndex;

    // public int MaterialPad0;
    // public int MaterialPad1;
    // public int MaterialPad2;

    public static MaterialConstants Default => new MaterialConstants
    {
        DiffuseAlbedo = Vector4.One,
        FresnelR0 = new Vector3(0.01f),
        Roughness = 64.0f,
        MatTransform = Matrix.Identity
    };
}
