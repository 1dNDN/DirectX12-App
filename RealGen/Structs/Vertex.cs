using System.Runtime.InteropServices;

using SharpDX;

namespace RealGen;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct Vertex
{
    public Vector3 Pos;
    public Vector4 Color;
}
