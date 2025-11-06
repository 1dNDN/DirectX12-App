using System;
using System.Collections.Generic;
using System.Linq;

using SharpDX;

namespace RealGen.Utils;

public class MeshData
{
    public List<BiggaVertex> Vertices { get; } = new List<BiggaVertex>();
    public List<int> Indices32 { get; } = new List<int>();

    public List<short> GetIndices16() => Indices32.Select(i => (short)i).ToList();

    public float AvgX => Vertices.Select(vertex => vertex.Position.X).Average();
    public float AvgY => Vertices.Select(vertex => vertex.Position.Y).Average();
    public float AvgZ => Vertices.Select(vertex => vertex.Position.Z).Average();

    public float MaxXYZ =>
        Vertices.Select(vertex =>
                Math.Max(
                    Math.Max(
                        Math.Abs(vertex.Position.X),
                        Math.Abs(vertex.Position.Y)),
                    Math.Abs(vertex.Position.Z)))
            .Max();

    public Matrix NormalizedWorld => Matrix.Translation(-AvgX, -AvgY, -AvgZ) * Matrix.Scaling(1 / MaxXYZ);
}
