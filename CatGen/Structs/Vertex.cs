using System.Runtime.InteropServices;

using SharpDX;

namespace CatGen;

/// <summary>
/// Вертекс для передачи в шейдер
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct BiggaVertex
{
    /// <summary>
    /// Позиция вертекса
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Нормаль вертекса
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    /// Координаты вертекса на текстуре
    /// </summary>
    public Vector2 TextureCoordinate;

    /// <summary>
    /// Касательная вертекса
    /// </summary>
    public Vector3 TangentU;


    /// <summary>
    /// Конструктор вертекса для передачи в шейдер
    /// </summary>
    /// <param name="position">Позиция вертекса</param>
    /// <param name="normal">Нормаль вертекса</param>
    /// <param name="tangentU">Касательная вертекса</param>
    /// <param name="textureCoordinate">Координаты вертекса на текстуре</param>
    public BiggaVertex(Vector3 position, Vector3 normal, Vector3 tangentU, Vector2 textureCoordinate)
    {
        Position = position;
        Normal = normal;
        TangentU = tangentU;
        TextureCoordinate = textureCoordinate;
    }

    public BiggaVertex(
        float positionX, float py, float pz,
        float nx, float ny, float nz,
        float tx, float ty, float tz,
        float u, float v) : this(
        new Vector3(positionX, py, pz),
        new Vector3(nx, ny, nz),
        new Vector3(tx, ty, tz),
        new Vector2(u, v))
    {
    }
}
