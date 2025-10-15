using System;

using SharpDX;

namespace RealGen.Utils;

public static class MathHelper
{
    private static readonly Random _random = new();

    public static int Rand(int minValue, int maxValue) =>
        _random.Next(minValue, maxValue);

    public static float Randf() =>
        _random.NextFloat(0.0f, 1.0f);

    public static float Randf(float minValue, float maxValue) =>
        _random.NextFloat(minValue, maxValue);

    public static float Sinf(double a) =>
        (float)Math.Sin(a);

    public static float Cosf(double d) =>
        (float)Math.Cos(d);

    public static float Tanf(double a) =>
        (float)Math.Tan(a);

    public static float Atanf(double d) =>
        (float)Math.Atan(d);

    public static float Atan2f(double y, double x) =>
        (float)Math.Atan2(y, x);

    public static float Acosf(double d) =>
        (float)Math.Acos(d);

    public static float Expf(double d) =>
        (float)Math.Exp(d);

    public static float Sqrtf(double d) =>
        (float)Math.Sqrt(d);
}
