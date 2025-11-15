using SharpDX;

namespace RealGen;

/// <summary>
/// Материал поверхности, описывающий отражаемость света от неё
/// </summary>
public class Material
{
    /// <summary>
    /// Уникальное имя материала для поиска
    /// </summary>
    public string Name { get; set; }

    // Index into constant buffer corresponding to this material.
    public int MaterialCBIndex { get; set; } = -1;

    // Index into SRV heap for diffuse texture.
    public int DiffuseSrvHeapIndex { get; set; } = -1;

    // Index into SRV heap for normal texture.
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
