using System.Collections.Generic;

namespace CatGen;

public static class AssetService
{
    private static readonly Dictionary<string, Texture> Textures = new();

    //TODO:
}

public enum MaterialsEnum
{
    Duck = 0,
    Semitransparent = 1,
    Fence = 2,
}

public enum TexturesEnum
{
    Duck = 0,
    Semitransparent = 1,
    Fence = 2,
}

public enum MeshEnum
{
    Duck = 0,
    // Semitransparent = 1,
    BigDragon = 2,
    Superbox = 3,
    Jenjina = 4,
    Cylinder = 5,
    Box = 6,
    Grid = 7,
    Sphere = 8,
}


public enum GeometryEnum
{
    Duck = 0,
    Semitransparent = 1,
    Fence = 2,
}

public enum PSOEnum
{
    Opaque = 0,
    OpaqueWireframe = 1,
    Transparent = 2,
    AlphaTested = 3,
}
