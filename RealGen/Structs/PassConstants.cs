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

    public Vector4 AmbientLight;

    public Vector4 FogColor;
    public float FogStart;
    public float FogRange;

    // Indices [0, NUM_DIR_LIGHTS) are directional lights;
    // indices [NUM_DIR_LIGHTS, NUM_DIR_LIGHTS+NUM_POINT_LIGHTS) are point lights;
    // indices [NUM_DIR_LIGHTS+NUM_POINT_LIGHTS, NUM_DIR_LIGHTS+NUM_POINT_LIGHT+NUM_SPOT_LIGHTS)
    // are spot lights for a maximum of MaxLights per object.
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Light.MaxLights)]
    public Light[] Lights;

    public static PassConstants Default => new PassConstants
    {
        View = Matrix.Identity,
        InvView = Matrix.Identity,
        Proj = Matrix.Identity,
        InvProj = Matrix.Identity,
        ViewProj = Matrix.Identity,
        InvViewProj = Matrix.Identity,
        NearZ = 1.0f,
        FarZ = 1000.0f,
        AmbientLight = Vector4.UnitW,
        FogColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        FogStart = 5.0f,
        FogRange = 150.0f,
        Lights = Light.DefaultArray
    };
}
