using SharpDX.Direct3D12;

namespace CatGen;

/// <summary>
/// Текстура для полигонов
/// </summary>
public class Texture : IDisposable
{
    /// <summary>
    /// Собсна сама текстурка как ресурс
    /// </summary>
    public Resource Resource { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        Resource?.Dispose();
        GC.SuppressFinalize(this);
    }
}
