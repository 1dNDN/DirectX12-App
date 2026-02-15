using SharpDX;

namespace CatGen;

/// <summary>
/// Материал поверхности, описывающий отражаемость света от неё
/// </summary>
public class Material
{
    public Material(Vector4 diffuseAlbedo, Vector3 fresnelR0, float roughness)
    {
        DiffuseAlbedo = diffuseAlbedo;
        FresnelR0 = fresnelR0;
        Roughness = roughness;

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
    /// Флаг, показывающий, что данные объекта изменились и нужно обновить буфер констант в каждом кадре.
    /// Таким образом, <c>NumFramesDirty = BaseDirectXWindow.NumFrameResources</c> при любом изменении.
    /// </summary>
    public int NumFramesDirty { get; set; } = BaseDirectXWindow.NumFrameResources;

    /// <summary>
    /// Коэффициент отражаемости поверхности
    /// </summary>
    public Vector4 DiffuseAlbedo { get; set; } = Vector4.One;

    /// <summary>
    /// Коэффициент эффекта Френеля для нормали
    /// </summary>
    public Vector3 FresnelR0 { get; set; } = new Vector3(0.01f);

    /// <summary>
    /// Шероховатость поверхности
    /// </summary>
    public float Roughness { get; set; } = 0.25f;

    public Matrix MatTransform { get; set; } = Matrix.Identity;
}
