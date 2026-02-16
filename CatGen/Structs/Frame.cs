using SharpDX.Direct3D12;

namespace CatGen;

/// <summary>
/// Все ресурсы, необходимые для отрисовки кадра. Нужно, чтобы рисовать независимо разные кадр с тройной буферизацией
/// </summary>
public class Frame : IDisposable
{
    internal Frame(Device device, int passCount, int objectCount, int materialCount)
    {
        CmdListAlloc = device.CreateCommandAllocator(CommandListType.Direct);
        PassConstantBuffer = new UploadBuffer<PassConstants>(device, passCount, true);
        MaterialConstantBuffer = new UploadBuffer<MaterialConstants>(device, materialCount, true);
        ObjectConstantBuffer = new UploadBuffer<ObjectConstants>(device, objectCount, true);
    }

    /// <summary>
    /// Каждому кадру нужен свой аллокатор команд. Нельзя делать reset аллокатора пока GPU обрабатывает команды.
    /// </summary>
    internal CommandAllocator CmdListAlloc { get; }

    /// <summary>
    /// Буфер констант, привязанных к кадру (например, матрица камеры)
    /// Каждому кадру нужен свой буфер констант. Нельзя обновлять буфер констант, пока GPU рисует кадр.
    /// </summary>
    internal UploadBuffer<PassConstants> PassConstantBuffer { get; }

    /// <summary>
    /// Буфер констант, привязанных к объекту
    /// Каждому кадру нужен свой буфер констант. Нельзя обновлять буфер констант, пока GPU рисует кадр.
    /// </summary>
    internal UploadBuffer<ObjectConstants> ObjectConstantBuffer { get; }

    /// <summary>
    /// Буфер констант с материалами
    /// Каждому кадру нужен свой буфер констант. Нельзя обновлять буфер констант, пока GPU рисует кадр.
    /// </summary>
    public UploadBuffer<MaterialConstants> MaterialConstantBuffer { get; }

    /// <summary>
    /// Барьер для синхронизации использования ресурсов кадра.
    /// Позволяет подождать, пока GPU освободит ресурсы.
    /// </summary>
    internal long Fence { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        ObjectConstantBuffer.Dispose();
        PassConstantBuffer.Dispose();
        CmdListAlloc.Dispose();
    }
}
