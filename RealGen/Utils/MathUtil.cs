using System;
using System.Numerics;

using SharpDX;

// ReSharper disable RedundantNameQualifier
// ReSharper disable UseSymbolAlias

namespace RealGen.Utils;

/// <summary>
/// Обёртки над математикой
/// </summary>
public static class MathHelper
{
    private static readonly Random _random = new();

    /// <summary>
    /// Случайное целое число в диапазоне
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public static int Rand(int minValue, int maxValue) =>
        _random.Next(minValue, maxValue);

    /// <summary>
    /// Случайное дробное число между 0.0 и 1.0
    /// </summary>
    /// <returns></returns>
    public static float Randf() =>
        _random.NextFloat(0.0f, 1.0f);

    /// <summary>
    /// Случайное дробное число в диапазоне
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public static float Randf(float minValue, float maxValue) =>
        _random.NextFloat(minValue, maxValue);

    /// <inheritdoc cref="Math.Sin"/>
    public static float Sinf(double a) =>
        (float)Math.Sin(a);

    /// <inheritdoc cref="Math.Cos"/>
    public static float Cosf(double d) =>
        (float)Math.Cos(d);

    /// <inheritdoc cref="Math.Tan"/>
    public static float Tanf(double a) =>
        (float)Math.Tan(a);

    /// <inheritdoc cref="Math.Atan"/>
    public static float Atanf(double d) =>
        (float)Math.Atan(d);

    /// <inheritdoc cref="Math.Atan2"/>
    public static float Atan2f(double y, double x) =>
        (float)Math.Atan2(y, x);

    /// <inheritdoc cref="Math.Acos"/>
    public static float Acosf(double d) =>
        (float)Math.Acos(d);

    /// <inheritdoc cref="Math.Exp"/>
    public static float Expf(double d) =>
        (float)Math.Exp(d);

    /// <inheritdoc cref="Math.Sqrt"/>
    public static float Sqrtf(double d) =>
        (float)Math.Sqrt(d);

    /// <summary>
    /// Каст в Matrix из Numerics
    /// </summary>
    public static Matrix4x4 ToMatrix4x4(Matrix matrix)
    {
        return new Matrix4x4(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        );
    }

    /// <summary>
    /// Каст в Matrix из SharpDX
    /// </summary>
    public static Matrix ToMatrix(this Matrix4x4 matrix)
    {
        return new Matrix(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        );
    }

    /// <summary>
    /// Каст в System.Numerics.Vector2
    /// </summary>
    public static System.Numerics.Vector2 ToVector2Numerics(this SharpDX.Vector2 vector)
    {
        return new System.Numerics.Vector2(vector.X, vector.Y);
    }

    /// <summary>
    /// Каст в SharpDX.Vector2
    /// </summary>
    public static SharpDX.Vector2 ToVector2SharpDX(this System.Numerics.Vector2 vector)
    {
        return new SharpDX.Vector2(vector.X, vector.Y);
    }

    /// <summary>
    /// Каст в System.Numerics.Vector3
    /// </summary>
    public static System.Numerics.Vector3 ToVector3Numerics(this SharpDX.Vector3 vector)
    {
        return new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
    }

    /// <summary>
    /// Каст в SharpDX.Vector3
    /// </summary>
    public static SharpDX.Vector3 ToVector3SharpDX(this System.Numerics.Vector3 vector)
    {
        return new SharpDX.Vector3(vector.X, vector.Y, vector.Z);
    }

    /// <summary>
    /// Каст в SharpDX.Vector3
    /// </summary>
    public static SharpDX.Vector3 ToVector3SharpDX(this SharpDX.Vector4 vector)
    {
        return new SharpDX.Vector3(vector.X, vector.Y, vector.Z);
    }

    /// <summary>
    /// Каст в System.Numerics.Vector4
    /// </summary>
    public static System.Numerics.Vector4 ToVector4Numerics(this SharpDX.Vector4 vector)
    {
        return new System.Numerics.Vector4(vector.X, vector.Y, vector.Z, vector.W);
    }

    /// <summary>
    /// Каст в SharpDX.Vector4
    /// </summary>
    public static SharpDX.Vector4 ToVector4Numerics(this System.Numerics.Vector4 vector)
    {
        return new SharpDX.Vector4(vector.X, vector.Y, vector.Z, vector.W);
    }

}
