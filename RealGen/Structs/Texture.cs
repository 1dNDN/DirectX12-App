using System;

using SharpDX.Direct3D12;

namespace RealGen;

/// <summary>
/// Текстура для полигонов
/// </summary>
public class Texture : IDisposable
{
    /// <summary>
    /// Уникальное имя текстуры для поиска
    /// </summary>
    public string Name { get; set; }

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
