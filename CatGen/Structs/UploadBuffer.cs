using System;
using System.Runtime.InteropServices;

using CatGen.Utils;

using SharpDX.Direct3D12;

namespace CatGen;

/// <summary>
/// Буфер для передачи данных на GPU
/// </summary>
/// <typeparam name="T"></typeparam>
public class UploadBuffer<T> : IDisposable where T : struct
{
    private readonly int _elementByteSize;
    private readonly IntPtr _resourcePointer;

    /// <summary>
    /// Конструктор буфера для передачи данных на гпу
    /// </summary>
    /// <param name="device"></param>
    /// <param name="elementCount"></param>
    /// <param name="isConstantBuffer"></param>
    public UploadBuffer(Device device, int elementCount, bool isConstantBuffer)
    {
        // Constant buffer elements need to be multiples of 256 bytes.
        // This is because the hardware can only view constant data
        // at m*256 byte offsets and of n*256 byte lengths.
        // typedef struct D3D12_CONSTANT_BUFFER_VIEW_DESC {
        // UINT64 OffsetInBytes; // multiple of 256
        // UINT   SizeInBytes;   // multiple of 256
        // } D3D12_CONSTANT_BUFFER_VIEW_DESC;
        _elementByteSize = isConstantBuffer
            ? BufferUtil.CalcConstantBufferByteSize<T>()
            : Marshal.SizeOf(typeof(T));

        Resource = device.CreateCommittedResource(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer(_elementByteSize * elementCount),
            ResourceStates.GenericRead);

        _resourcePointer = Resource.Map(0);

        // We do not need to unmap until we are done with the resource. However, we must not write to
        // the resource while it is in use by the GPU (so we must use synchronization techniques).
    }

    /// <summary>
    /// Ресурс буфера
    /// </summary>
    public Resource Resource { get; }

    /// <summary>
    /// Копирует данные из памяти в ресурс буфера
    /// </summary>
    /// <param name="elementIndex">Индекс данных в буфере</param>
    /// <param name="data">Данные для передачи</param>
    public void CopyData(int elementIndex, ref T data) =>
        Marshal.StructureToPtr(data, _resourcePointer + elementIndex * _elementByteSize, true);

    /// <inheritdoc />
    public void Dispose()
    {
        Resource.Unmap(0);
        Resource.Dispose();

        GC.SuppressFinalize(this);
    }
}
