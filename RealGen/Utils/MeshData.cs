using System.Collections.Generic;
using System.Linq;

namespace RealGen.Utils;

public class MeshData
{
    public List<BiggaVertex> Vertices { get; } = new List<BiggaVertex>();
    public List<int> Indices32 { get; } = new List<int>();

    public List<short> GetIndices16() => Indices32.Select(i => (short)i).ToList();
}
