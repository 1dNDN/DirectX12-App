using CatGen.Utils;

using SharpDX;

using Point = SharpDX.Point;

namespace CatGen;

/// <summary>
/// Класс камеры
/// </summary>
public class Camera
{
    private readonly GameTimer _timer;
    private bool _viewDirty = true;

    /// <summary>
    /// Конструктор камеры
    /// </summary>
    public Camera(GameTimer timer)
    {
        _timer = timer;
        SetLens(MathUtil.PiOverFour, 1.0f, 1.0f, 1000.0f);
    }

    /// <summary>
    /// Координаты камеры
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Юнит-вектор направо от камеры
    /// </summary>
    public Vector3 Right { get; private set; } = Vector3.UnitX;

    /// <summary>
    /// Юнит-вектор вверх от камеры
    /// </summary>
    public Vector3 Up { get; private set; } = Vector3.UnitY;

    /// <summary>
    /// Юнит-вектор прямо от камеры
    /// </summary>
    public Vector3 Look { get; private set; } = Vector3.UnitZ;

    /// <summary>
    /// Ближняя плоскость culling
    /// </summary>
    public float NearZ { get; private set; }

    /// <summary>
    /// Дальняя плоскость culling
    /// </summary>
    public float FarZ { get; private set; }

    /// <summary>
    /// Соотношение сторон экрана
    /// </summary>
    public float Aspect { get; private set; }

    /// <summary>
    /// Угол обзора по высоте
    /// </summary>
    public float FovY { get; private set; }

    /// <summary>
    /// Угол обзора по ширине
    /// </summary>
    public float FovX
    {
        get
        {
            var halfWidth = 0.5f * NearWindowWidth;
            return 2.0f * MathHelper.Atanf(halfWidth / NearZ);
        }
    }
    public float NearWindowHeight { get; private set; }
    public float NearWindowWidth => Aspect * NearWindowHeight;
    public float FarWindowHeight { get; private set; }
    public float FarWindowWidth => Aspect * FarWindowHeight;

    /// <summary>
    /// Матрица камеры
    /// </summary>
    public Matrix View { get; private set; } = Matrix.Identity;

    /// <summary>
    /// Матрица проекции камеры
    /// </summary>
    public Matrix Proj { get; private set; } = Matrix.Identity;

    public Matrix ViewProj => View * Proj;
    public BoundingFrustum Frustum => new BoundingFrustum(ViewProj);

    /// <summary>
    /// Установка параметров камеры
    /// </summary>
    /// <param name="fovY">Угол обзора по высоте</param>
    /// <param name="aspect">Соотношение сторон экрана</param>
    /// <param name="nearZ">Ближняя плоскость culling</param>
    /// <param name="farZ">Дальняя плоскость culling</param>
    public void SetLens(float fovY, float aspect, float nearZ, float farZ)
    {
        FovY = fovY;
        Aspect = aspect;
        NearZ = nearZ;
        FarZ = farZ;

        NearWindowHeight = 2.0f * nearZ * MathHelper.Tanf(0.5f * fovY);
        FarWindowHeight = 2.0f * farZ * MathHelper.Tanf(0.5f * fovY);

        Proj = Matrix.PerspectiveFovLH(fovY, aspect, nearZ, farZ);
    }

    public void LookAt(Vector3 pos, Vector3 target, Vector3 up)
    {
        Position = pos;
        Look = Vector3.Normalize(target - pos);
        Right = Vector3.Normalize(Vector3.Cross(up, Look));
        Up = Vector3.Cross(Look, Right);
        _viewDirty = true;
    }

    /// <summary>
    /// Движение вправо
    /// </summary>
    /// <param name="d">Сколько движения</param>
    public void Strafe(float d)
    {
        Position += Right * d;
        _viewDirty = true;
    }

    /// <summary>
    /// Движение вперёд
    /// </summary>
    /// <param name="d">Сколько движения</param>
    public void Walk(float d)
    {
        Position += Look * d;
        _viewDirty = true;
    }

    /// <summary>
    /// Движение вверх
    /// </summary>
    /// <param name="d">Сколько движения</param>
    public void MoveUp(float d)
    {
        Position += Up * d;
        _viewDirty = true;
    }

    public void Pitch(float angle)
    {
        // Rotate up and look vector about the right vector.

        var r = Matrix.RotationAxis(Right, angle);

        Up = Vector3.TransformNormal(Up, r);
        Look = Vector3.TransformNormal(Look, r);

        _viewDirty = true;
    }

    public void RotateY(float angle)
    {
        // Rotate the basis vectors about the world y-axis.

        var r = Matrix.RotationY(angle);

        Right = Vector3.TransformNormal(Right, r);
        Up = Vector3.TransformNormal(Up, r);
        Look = Vector3.TransformNormal(Look, r);

        _viewDirty = true;
    }

    public void UpdateViewMatrix()
    {
        if (!_viewDirty) return;

        // Keep camera's axes orthogonal to each other and of unit length.
        Look = Vector3.Normalize(Look);
        Up = Vector3.Normalize(Vector3.Cross(Look, Right));

        // U, L already ortho-normal, so no need to normalize cross product.
        Right = Vector3.Cross(Up, Look);

        // Fill in the view matrix entries.
        var x = -Vector3.Dot(Position, Right);
        var y = -Vector3.Dot(Position, Up);
        var z = -Vector3.Dot(Position, Look);

        View = new Matrix(
            Right.X, Up.X, Look.X, 0.0f,
            Right.Y, Up.Y, Look.Y, 0.0f,
            Right.Z, Up.Z, Look.Z, 0.0f,
            x, y, z, 1.0f
        );

        _viewDirty = false;
    }

    public Ray GetPickingRay(Point sp, int clientWidth, int clientHeight)
    {
        var p = Proj;

        // Convert screen pixel to view space.
        var vx = (2f * sp.X / clientWidth - 1f) / p.M11;
        var vy = (-2f * sp.Y / clientHeight + 1f) / p.M22;

        var ray = new Ray(Vector3.Zero, new Vector3(vx, vy, 1));
        var v = View;
        var invView = Matrix.Invert(v);

        var toWorld = invView;

        ray = new Ray(
            Vector3.TransformCoordinate(ray.Position, toWorld),
            Vector3.TransformNormal(ray.Direction, toWorld));

        return ray;
    }

    /// <summary>
    /// Обновляет состояние камеры по сравнению с прошлым тиком
    /// </summary>
    public void Update()
    {
        var dx = 10.0f;
        var dt = _timer.DeltaTime;

        if (KeyboardUtil.IsKeyDown(Keys.LControlKey))
            dx *= 3.0f;

        if (KeyboardUtil.IsKeyDown(Keys.W))
            Walk(dx * dt);

        if (KeyboardUtil.IsKeyDown(Keys.S))
            Walk(-dx * dt);

        if (KeyboardUtil.IsKeyDown(Keys.A))
            Strafe(-dx * dt);

        if (KeyboardUtil.IsKeyDown(Keys.D))
            Strafe(dx * dt);

        if (KeyboardUtil.IsKeyDown(Keys.Space))
            MoveUp(dx * dt);

        if (KeyboardUtil.IsKeyDown(Keys.LShiftKey))
            MoveUp(-dx * dt);

        UpdateViewMatrix();
    }
}
