using System.Drawing;
using System.Linq;

namespace CatGen.AssetServices;

public class PNGReader
{
    public static Image ImportTexture(string filepath)
    {
        var bitmap = Image.FromFile(filepath);
        return bitmap;
    }
}
