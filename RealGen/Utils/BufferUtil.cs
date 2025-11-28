using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.Direct3D12;

namespace RealGen.Utils;

/// <summary>
/// Методы для работы с буферами
/// </summary>
public static class BufferUtil
{
    /// <summary>
    /// Создаёт буфер для передачи данных в GPU
    /// </summary>
    /// <param name="device"></param>
    /// <param name="cmdList"></param>
    /// <param name="initData"></param>
    /// <param name="byteSize"></param>
    /// <param name="uploadBuffer"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Resource CreateDefaultBuffer<T>(
        Device device,
        GraphicsCommandList cmdList,
        T[] initData,
        long byteSize,
        out Resource uploadBuffer) where T : struct
    {
        // Create the actual default buffer resource.
        var defaultBuffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer(byteSize),
            ResourceStates.Common);

        // In order to copy CPU memory data into our default buffer, we need to create
        // an intermediate upload heap.
        uploadBuffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer(byteSize),
            ResourceStates.GenericRead);

        // Copy the data to the upload buffer.
        var ptr = uploadBuffer.Map(0);
        Utilities.Write(ptr, initData, 0, initData.Length);
        uploadBuffer.Unmap(0);

        // Schedule to copy the data to the default buffer resource.
        cmdList.ResourceBarrierTransition(defaultBuffer, ResourceStates.Common, ResourceStates.CopyDestination);
        cmdList.CopyResource(defaultBuffer, uploadBuffer);
        cmdList.ResourceBarrierTransition(defaultBuffer, ResourceStates.CopyDestination, ResourceStates.GenericRead);

        // Note: uploadBuffer has to be kept alive after the above function calls because
        // the command list has not been executed yet that performs the actual copy.
        // The caller can Release the uploadBuffer after it knows the copy has been executed.

        return defaultBuffer;
    }

    /// <summary>
    ///     Constant buffers must be a multiple of the minimum hardware
    ///     allocation size (usually 256 bytes). So round up to nearest
    ///     multiple of 256. We do this by adding 255 and then masking off
    ///     the lower 2 bytes which store all bits &lt; 256. <br />
    ///     Example: Suppose byteSize = 300. <br />
    ///     (300 + 255) &amp; ~255 <br />
    ///     555 &amp; ~255 <br />
    ///     0x022B &amp; ~0x00ff <br />
    ///     0x022B &amp; 0xff00 <br />
    ///     0x0200 <br />
    ///     512 <br />
    /// </summary>
    public static int CalcConstantBufferByteSize<T>() where T : struct =>
        Marshal.SizeOf(typeof(T)) + 255 & ~255;
}
