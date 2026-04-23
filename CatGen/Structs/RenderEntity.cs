using SharpDX;

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace CatGen;

/// <summary>
/// Класс конкретного объекта в мире - тот экземпляр, который конкретно рендерится
/// </summary>
public class RenderEntity : Dirtyable
{
    /// <summary>
    /// ID энтити, которое рендерим
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Модель этого объекта с мешами, текстурами и материалами
    /// </summary>
    public Prefab EntityModel { get; set; }

    private Matrix? _world;
    private Matrix _baseWorld = Matrix.Identity;

    /// <summary>
    /// Матрица, описывающая сущность относительно мира. Произведение матрицы мира сабмеша на матрицу мира сущности
    /// </summary>
    public Matrix World => _world ??= EntityModel.World * BaseWorld;

    /// <summary>
    /// Матрица, описывающая саму сущность относительно мира. В частности, позицию, направление и масштаб
    /// </summary>
    public Matrix BaseWorld
    {
        get {
            return _baseWorld;
        }
        set {
            _world = null;
            _baseWorld = value;
            Dirty();
        }
    }

    /// <summary>
    /// Индекс объекта в буфере констант для этого объекта
    /// </summary>
    public int ObjCbOffset { get; set; } = -1;

    public Matrix TexTransform { get; set; } = Matrix.Identity;
}
