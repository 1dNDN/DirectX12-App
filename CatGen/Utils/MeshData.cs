using System;
using System.Collections.Generic;
using System.Linq;

using SharpDX;

namespace CatGen.Utils;

/// <summary>
/// Данные о геометрии независимого объекта
/// </summary>
public class MeshData
{
    /// <summary>
    /// Вершины геометрии объекта
    /// </summary>
    public List<BiggaVertex> Vertices { get; } = [];

    /// <summary>
    /// Индексы геометрии объекта
    /// </summary>
    public List<int> Indices { get; } = [];

    /// <summary>
    /// Координата средней точки объекта
    /// </summary>
    public Vector3 AvgLocation =>
        new(
            Vertices.Select(vertex => vertex.Position.X).Average(),
            Vertices.Select(vertex => vertex.Position.Y).Average(),
            Vertices.Select(vertex => vertex.Position.Z).Average()
        );

    /// <summary>
    /// Размер объекта по крайним точкам
    /// </summary>
    public float Size =>
        Vertices.Select(vertex =>
                Math.Max(
                    Math.Max(
                        Math.Abs(vertex.Position.X),
                        Math.Abs(vertex.Position.Y)),
                    Math.Abs(vertex.Position.Z)))
            .Max();

    /// <summary>
    /// Матрица нормализованного мира объекта. Центрирует объект в 0,0,0 и выставляет масштаб так, что размер 1.
    /// </summary>
    public Matrix NormalizedWorld => Matrix.Translation(-AvgLocation) * Matrix.Scaling(1 / Size);
}
