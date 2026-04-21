namespace CatGen;

/// <summary>
/// Абстрактный класс для всех сущностей, которые можно сделать Dirty
/// </summary>
public abstract class Dirtyable
{
    // Dirty flag indicating the object data has changed and we need to update the constant buffer.
    // Because we have an object cbuffer for each FrameResource, we have to apply the
    // update to each FrameResource. Thus, when we modify obect data we should set
    // NumFramesDirty = gNumFrameResources so that each frame resource gets the update.

    /// <summary>
    /// Флаг, показывающий, что данные объекта изменились и нужно обновить буфер констант в каждом кадре.
    /// Таким образом, <c>NumFramesDirty = BaseDirectXWindow.NumFrameResources</c> при любом изменении.
    /// </summary>
    public int NumFramesDirty { get; set; } = BaseDirectXWindow.NumFrameResources;

    /// <summary>
    /// Объект изменился, теперь он дёрти и надо грузить снова
    /// </summary>
    public void Dirty()
    {
        NumFramesDirty = BaseDirectXWindow.NumFrameResources;
    }
}
