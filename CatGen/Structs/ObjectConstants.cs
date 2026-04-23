using System.Runtime.InteropServices;

using SharpDX;

namespace CatGen
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct ObjectConstants
    {
        public Matrix World;
        public Matrix TexTransform;
        
        public int MaterialIndex;
        public int ObjPad0;
        public int ObjPad1;
        public int ObjPad2;
    }
}
