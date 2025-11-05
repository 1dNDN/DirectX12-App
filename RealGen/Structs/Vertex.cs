using System.Runtime.InteropServices;

using SharpDX;

namespace RealGen;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct BiggaVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector3 TangentU;
    public Vector2 TexC;

    public BiggaVertex(Vector3 p, Vector3 n, Vector3 t, Vector2 uv)
    {
        Position = p;
        Normal = n;
        TangentU = t;
        TexC = uv;
    }

    public BiggaVertex(
        float px, float py, float pz,
        float nx, float ny, float nz,
        float tx, float ty, float tz,
        float u, float v) : this(
        new Vector3(px, py, pz),
        new Vector3(nx, ny, nz),
        new Vector3(tx, ty, tz),
        new Vector2(u, v))
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SmallaVertex
{
    public Vector3 Pos;
    public Vector4 Color;
}
