using System.Runtime.InteropServices;

using SharpDX;
namespace RealGen;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PassConstants
{
    public Matrix View;
    public Matrix InvView;
    public Matrix Proj;
    public Matrix InvProj;
    public Matrix ViewProj;
    public Matrix InvViewProj;
    public Vector3 EyePosW;
    public float PerObjectPad1;
    public Vector2 RenderTargetSize;
    public Vector2 InvRenderTargetSize;
    public float NearZ;
    public float FarZ;
    public float TotalTime;
    public float DeltaTime;
}
