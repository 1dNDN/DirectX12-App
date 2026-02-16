using HashidsNet;

namespace CatGen.Common;

public static class IdGenerator
{
    private static readonly Hashids _hashids = new("CG", 0, "abcdefghijklmnopqrstuvwxyz1234567890", "cfhistu");

    public static string NewGuid()
    {
        return _hashids.EncodeLong(Math.Abs(DateTime.Now.Ticks));
    }
}
