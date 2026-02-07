using HashidsNet;

namespace CatGen.Common;

public static class IdGenerator
{
    private static IHashids hashids = new Hashids("CG", 0, "abcdefghijklmnopqrstuvwxyz1234567890", "cfhistu");  //MLHIDE

    public static string NewGuid()
    {
        return hashids.EncodeLong(Math.Abs(DateTime.Now.ToBinary()));
    }
}
