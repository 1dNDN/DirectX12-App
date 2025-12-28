using System.Drawing;
using System.Linq;

namespace RealGen.AssetServices;

public class PNGReader
{
    public static Image ImportTexture(string filepath)
    {
        var bitmap = Image.FromFile(filepath);
        return bitmap;
    }
}
