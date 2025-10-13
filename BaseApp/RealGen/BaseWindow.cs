using System;
using System.Drawing;
using System.Windows.Forms;

namespace RealGen;

/// <summary>
/// Базовая реализация окошка, в куда рендерить
/// </summary>
public class BaseWindow : IDisposable
{
    /// <summary>
    /// Заголовок окна
    /// </summary>
    protected string WindowTitle { get; set; } = "DdndGen Demo";

    /// <summary>
    /// Ширина окна
    /// </summary>
    protected int Width { get; set; } = 1280;

    /// <summary>
    /// Высота окна
    /// </summary>
    protected int Height { get; set; } = 720;

    /// <summary>
    /// Базовая формочка окна
    /// </summary>
    protected Form Window;

    /// <summary>
    /// Запущено ли приложение?
    /// </summary>
    public bool Running { get; protected set; }

    /// <summary>
    /// Поставлено ли приложение на паузу?
    /// </summary>
    public bool AppPaused { get; set; }

    /// <summary>
    /// Свёрнуто ли приложение?
    /// </summary>
    public bool Minimized { get; private set; }

    /// <summary>
    /// Развёрнуто ли приложение?
    /// </summary>
    public bool Maximized { get; private set; }

    /// <summary>
    /// Находится ли приложение в состоянии изменения размера?
    /// </summary>
    public bool Resizing { get; private set; }

    public GameTimer Timer { get; } = new GameTimer();

    /// <summary>
    /// Последнее состояние окна Windows Forms
    /// </summary>
    protected FormWindowState LastWindowState = FormWindowState.Normal;

    /// <summary>
    /// Инициализация
    /// </summary>
    public virtual void Init()
    {
        Window = new Form
        {
            Text = WindowTitle,
            Name = "DdndGen",
            FormBorderStyle = FormBorderStyle.Sizable,
            ClientSize = new Size(Width, Height),
            StartPosition = FormStartPosition.CenterScreen,
            MinimumSize = new Size(200, 200),
        };

        Window.MouseDown += (sender, e) => OnMouseDown(e.Button, new Point(e.X, e.Y));
        Window.MouseUp += (sender, e) => OnMouseUp(e.Button, new Point(e.X, e.Y));
        Window.MouseMove += (sender, e) => OnMouseMove(e.Button, new Point(e.X, e.Y));
        Window.KeyDown += (sender, e) => OnKeyDown(e.KeyCode);
        Window.KeyUp += (sender, e) => OnKeyUp(e.KeyCode);
        Window.ResizeBegin += OnWindowOnResizeBegin;
        Window.ResizeEnd += OnWindowOnResizeEnd;
        Window.Resize += OnWindowOnResize;
        Window.Activated += OnWindowOnActivated;
        Window.Deactivate += OnWindowOnDeactivate;
        Window.HandleDestroyed += (sender, e) => OnHandleDestroyed();

        Window.Show();
        Window.Update();
    }

    protected virtual void OnWindowOnResize(object sender, EventArgs e)
    {
        Width = Window.ClientSize.Width;
        Height = Window.ClientSize.Height;

        // Когда состояние окна меняется
        if (Window.WindowState != LastWindowState)
        {
            LastWindowState = Window.WindowState;
            switch (Window.WindowState)
            {
                case FormWindowState.Maximized:
                    AppPaused = false;
                    Minimized = false;
                    Maximized = true;
                    OnResizeInternal();
                    break;
                case FormWindowState.Minimized:
                    AppPaused = true;
                    Minimized = true;
                    Maximized = false;
                    break;
                // Разворачиваемся из состояния "свёрнуто"
                case FormWindowState.Normal when Minimized:
                    AppPaused = false;
                    Minimized = false;
                    OnResizeInternal();
                    break;
                // Разворачиваемся из состояния "развёрнуто"
                case FormWindowState.Normal when Maximized:
                    AppPaused = false;
                    Maximized = false;
                    OnResizeInternal();
                    break;
                case FormWindowState.Normal when Resizing:
                    // Если пользователь перетаскивает полосы изменения размера, мы не изменяем
                    // размеры буферов здесь, потому что при непрерывном перетаскивании
                    // полос изменения размера в окно отправляется поток сообщений WM_SIZE,
                    // и было бы бессмысленно (и медленно) изменять размеры для каждого
                    // полученного сообщения WM_SIZE во время перетаскивания.
                    // Вместо этого мы выполняем сброс после того, как пользователь
                    // завершит изменение размера окна и отпустит полосы изменения размера,
                    // что отправит сообщение WM_EXITSIZEMOVE.
                    break;
                // Вызов API вроде SetWindowPos или mSwapChain->SetFullscreenState.
                case FormWindowState.Normal:
                    OnResizeInternal();
                    break;
            }
        }
        // Изменение размера из-за привязки к другим окнам или границе экрана
        else if (!Resizing)
        {
            OnResizeInternal();
        }
    }

    protected virtual void OnMouseDown(MouseButtons button, Point location)
    {
        Window.Capture = true;
    }

    protected virtual void OnMouseUp(MouseButtons button, Point location)
    {
        Window.Capture = false;
    }

    protected virtual void OnMouseMove(MouseButtons button, Point location)
    {
    }

    protected virtual void OnKeyDown(Keys keyCode)
    {
    }

    protected virtual void OnKeyUp(Keys keyCode)
    {
        switch (keyCode)
        {
            case Keys.Escape:
                Running = false;
                break;
        }
    }

    protected virtual void OnWindowOnResizeBegin(object sender, EventArgs e)
    {
        AppPaused = true;
        Resizing = true;
        Timer.Stop();
    }

    protected virtual void OnWindowOnResizeEnd(object sender, EventArgs e)
    {
        AppPaused = false;
        Resizing = false;
        Timer.Start();
        OnResizeInternal();
    }
    protected virtual void OnWindowOnActivated(object sender, EventArgs e)
    {
        AppPaused = false;
        Timer.Start();
    }

    protected virtual void OnWindowOnDeactivate(object sender, EventArgs e)
    {
        AppPaused = true;
        Timer.Stop();
    }

    protected virtual void OnHandleDestroyed()
    {
        Running = false;
    }

    protected virtual void OnResizeInternal()
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}