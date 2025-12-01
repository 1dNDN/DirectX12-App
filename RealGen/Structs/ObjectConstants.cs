using System.Runtime.InteropServices;

using SharpDX;

namespace RealGen
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct ObjectConstants
    {
        public Matrix World;
        public Matrix TexTransform;
    }
}
