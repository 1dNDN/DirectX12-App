using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

using SharpDX.Direct3D12;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D12.Device;
using Image = SharpGLTF.Schema2.Image;
using Resource = SharpDX.Direct3D12.Resource;

namespace CatGen.Utils;

public static class TextureUtil
{
    public const int DefaultShader4ComponentMapping = 5768;

    /// <summary>
    ///     Load texture from inmemory stream from gltf
    /// </summary>
    /// <param name="device">Device</param>
    /// <param name="texture">Texture</param>
    /// <returns></returns>
    public static Resource CreateTextureFromPNG(Device device, Image texture)
    {
        using var textureStream = texture.Content.Open();

        var bitmap = new Bitmap(textureStream);

        var width = bitmap.Width;
        var height = bitmap.Height;

        var textureDesc = new ResourceDescription
        {
            MipLevels = 1,
            Format = Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            Flags = ResourceFlags.None,
            DepthOrArraySize = 1,
            SampleDescription = new SampleDescription(1, 0),
            Dimension = ResourceDimension.Texture2D,
        };

        var buffer = device.CreateCommittedResource(new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0), HeapFlags.None, textureDesc, ResourceStates.GenericRead);

        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        buffer.WriteToSubresource(
            0,
            new ResourceRegion
            {
                Back = 1,
                Bottom = height,
                Right = width,
            },
            data.Scan0,
            4 * width,
            4 * width * height);
        int bufferSize = data.Height * data.Stride;

        bitmap.UnlockBits(data);

        return buffer;
    }
}
