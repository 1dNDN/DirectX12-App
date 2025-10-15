using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D12.Device;
using Feature = SharpDX.Direct3D12.Feature;
using Resource = SharpDX.Direct3D12.Resource;

namespace RealGen;

public class DirectXWindow : BaseWindow
{
    /// <summary>
    /// Фабрика DXGI
    /// </summary>
    protected Factory4 DXGIFactory { get; private set; }

    /// <summary>
    /// Адаптер, на котором будем рендерить
    /// </summary>
    protected Device RenderDevice { get; private set; }

    /// <summary>
    /// Барьер GPU
    /// </summary>
    protected Fence RenderFence { get; private set; }

    /// <summary>
    /// Текущая точка барьера синхронизации GPU
    /// </summary>
    protected long CurrentFence { get; set; }

    private AutoResetEvent FenceEvent { get; set; }

    /// <summary>
    /// Размер дескрипторов render target resources
    /// </summary>
    protected int RtvDescriptorSize { get; private set; }

    /// <summary>
    /// Размер дескрипторов depth/stencil resources
    /// </summary>
    protected int DsvDescriptorSize { get; private set; }

    /// <summary>
    /// Размер дескрипторов constant buffers, shader resources и unordered access view resources
    /// </summary>
    protected int CbvSrvUavDescriptorSize { get; private set; }

    /// <summary>
    /// Количество дескрипторов render target resources
    /// </summary>
    protected virtual int RtvDescriptorCount => SwapChainBufferCount;

    /// <summary>
    /// Количество дескрипторов depth/stencil resources
    /// </summary>
    protected virtual int DsvDescriptorCount => 1;

    /// <summary>
    /// Очередь списков команд
    /// </summary>
    protected CommandQueue RenderCommandQueue { get; set; }

    /// <summary>
    /// Аллокатор команд для списка команд GPU
    /// </summary>
    protected CommandAllocator RenderDirectCmdListAlloc { get; set; }

    /// <summary>
    /// Список команд для GPU
    /// </summary>
    protected GraphicsCommandList RenderCommandList { get; set; }

    /// <summary>
    /// SwapChain для буферизации
    /// </summary>
    protected SwapChain3 RenderSwapChain { get; private set; }

    /// <summary>
    /// Список буферов render target для SwapChain
    /// </summary>
    private readonly Resource[] _swapChainBuffers = new Resource[SwapChainBufferCount];

    /// <summary>
    /// Буфер depth/stencil resources
    /// </summary>
    protected Resource DepthStencilBuffer { get; private set; }

    /// <summary>
    /// Куча дескрипторов render target resources
    /// </summary>
    protected DescriptorHeap RenderRtvHeap { get; private set; }

    /// <summary>
    /// Куча дескрипторов depth/stencil resources
    /// </summary>
    protected DescriptorHeap RenderDsvHeap { get; private set; }

    /// <summary>
    /// Viewport, в куда рендерить
    /// </summary>
    protected ViewportF RenderViewport { get; set; }

    /// <summary>
    /// Прямоугольник, в куда рендерить
    /// </summary>
    protected RectangleF ScissorRectangle { get; set; }

    protected Format BackBufferFormat { get; } = Format.R8G8B8A8_UNorm;
    protected Format DepthStencilFormat { get; } = Format.D24_UNorm_S8_UInt;

    /// <summary>
    /// Текущий BackBuffer
    /// </summary>
    protected Resource CurrentBackBuffer => _swapChainBuffers[RenderSwapChain.CurrentBackBufferIndex];

    /// <summary>
    /// Текущий дескриптор на BackBuffer
    /// </summary>
    protected CpuDescriptorHandle CurrentBackBufferView => RenderRtvHeap.CPUDescriptorHandleForHeapStart + RenderSwapChain.CurrentBackBufferIndex * RtvDescriptorSize;

    /// <summary>
    /// Текущий дескриптор на DepthStensil Buffer
    /// </summary>
    protected CpuDescriptorHandle DepthStencilView => RenderDsvHeap.CPUDescriptorHandleForHeapStart;

    /// <summary>
    /// Включить ли 4X MSAA
    /// </summary>
    private bool _m4xMsaaState;

    /// <summary>
    /// Уровень качества 4X MSAA
    /// </summary>
    private int _m4xMsaaQuality;

    /// <summary>
    /// Включен ли MSAA 4X
    /// </summary>
    protected bool M4xMsaaState
    {
        get => _m4xMsaaState;
        set
        {
            if (_m4xMsaaState != value)
            {
                _m4xMsaaState = value;

                if (Running)
                {
                    // Нужно пересоздать swapchain и всю херню с новыми настройками
                    CreateSwapChain();
                    OnResizeInternal();
                }
            }
        }
    }

    /// <summary>
    /// Уровень сглаживания MSAA
    /// </summary>
    protected int MsaaCount => M4xMsaaState ? 4 : 1;

    /// <summary>
    /// Качество 4X MSAA
    /// </summary>
    protected int MsaaQuality => M4xMsaaState ? _m4xMsaaQuality - 1 : 0;

    protected const int SwapChainBufferCount = 2;


    /// <<inheritdoc/>
    public override void Init()
    {
        // Инициализируем формочку винды
        base.Init();

        CheckDebugLayer();

        DXGIFactory = new Factory4();
        RenderDevice = GetRenderDevice();

        RenderFence = RenderDevice.CreateFence(0, FenceFlags.None);
        FenceEvent = new AutoResetEvent(false);

        InitDescriptorSizes();
        InitMSAASettings();
        CreateCommandObjects();
        CreateSwapChain();
        CreateRtvAndDsvDescriptorHeaps();

        OnResizeInternal();

        Running = true;
    }

    /// <summary>
    /// Залупливает приложение
    /// </summary>
    public virtual void Run()
    {
        Timer.Reset();
        while (Running)
        {
            Application.DoEvents();
            Timer.Tick();
            if (!AppPaused)
            {
                CalculateFrameRateStats();
                Update(Timer);
                Draw(Timer);
            }
            else
            {
                Thread.Sleep(25);
            }
        }
    }

    /// <summary>
    /// Обновляет состояние приложения
    /// </summary>
    /// <param name="timer"></param>
    protected virtual void Update(GameTimer timer)
    {

    }

    /// <summary>
    /// Залупленно отрисовывает картинку
    /// </summary>
    /// <param name="timer"></param>
    protected virtual void Draw(GameTimer timer)
    {

    }

    /// <summary>
    /// Пытаемся получить адаптер для рендеринга
    /// </summary>
    /// <returns></returns>
    private Device GetRenderDevice()
    {
        try
        {
            // Пытаемся создать аппаратное устройство.
            // NULL передается для использования адаптера по умолчанию - первого адаптера,
            // найденного при перечислении Factory.Adapters.
            return new Device(null, FeatureLevel.Level_11_0);
        }
        catch (SharpDXException)
        {
            // Откат к софтварной реализации (WARP)
            var warpAdapter = DXGIFactory.GetWarpAdapter();
            return new Device(warpAdapter, FeatureLevel.Level_11_0);
        }
    }

    /// <summary>
    /// Инициализируем размеры дескрипторов для буферов
    /// </summary>
    private void InitDescriptorSizes()
    {

        RtvDescriptorSize = RenderDevice.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        DsvDescriptorSize = RenderDevice.GetDescriptorHandleIncrementSize(DescriptorHeapType.DepthStencilView);
        CbvSrvUavDescriptorSize = RenderDevice.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
    }

    /// <summary>
    /// Инициализация сглаживания MSAA
    /// </summary>
    private void InitMSAASettings()
    {
        // Проверяем поддержку 4X MSAA для формата нашего заднего буфера.
        // Все устройства с поддержкой Direct3D 11 обеспечивают 4X MSAA для всех
        // форматов, поэтому нам необходимо проверить только уровень качества.
        FeatureDataMultisampleQualityLevels msQualityLevels;
        msQualityLevels.Format = BackBufferFormat;
        msQualityLevels.SampleCount = 4;
        msQualityLevels.Flags = MultisampleQualityLevelFlags.None;
        msQualityLevels.QualityLevelCount = 0;
        Debug.Assert(RenderDevice.CheckFeatureSupport(Feature.MultisampleQualityLevels, ref msQualityLevels));
        _m4xMsaaQuality = msQualityLevels.QualityLevelCount;

    }

    /// <summary>
    /// Создаём список команд для GPU
    /// </summary>
    private void CreateCommandObjects()
    {
        var queueDesc = new CommandQueueDescription(CommandListType.Direct);
        RenderCommandQueue = RenderDevice.CreateCommandQueue(queueDesc);

        RenderDirectCmdListAlloc = RenderDevice.CreateCommandAllocator(CommandListType.Direct);

        RenderCommandList = RenderDevice.CreateCommandList(
            0,
            CommandListType.Direct,
            RenderDirectCmdListAlloc, // Associated command allocator.
            null);              // Initial PipelineStateObject.

        // Изначально устанавливаем закрытое состояние. Это необходимо, потому что при первом
        // использовании списка команд мы будем вызывать Reset, а для вызова
        // Reset список должен находиться в закрытом состоянии.
        RenderCommandList.Close();
    }

    /// <summary>
    /// Создаём SwapChain для буферов рендеринга
    /// </summary>
    private void CreateSwapChain()
    {
        // Диспозим предыдущий swapchain перед его пересозданием.
        RenderSwapChain?.Dispose();

        var description = new SwapChainDescription
        {
            ModeDescription = new ModeDescription
            {
                Width = Width,
                Height = Height,
                Format = BackBufferFormat,
                //TODO: убрать это говно!
                RefreshRate = new Rational(170, 1),
                Scaling = DisplayModeScaling.Unspecified,
                ScanlineOrdering = DisplayModeScanlineOrder.Unspecified,
            },
            SampleDescription = new SampleDescription
            {
                Count = 1,
                Quality = 0,
            },
            Usage = Usage.RenderTargetOutput,
            BufferCount = SwapChainBufferCount,
            SwapEffect = SwapEffect.FlipDiscard,
            Flags = SwapChainFlags.AllowModeSwitch,
            OutputHandle = Window.Handle,
            IsWindowed = true,
        };

        using var tempSwapChain = new SwapChain(DXGIFactory, RenderCommandQueue, description);
        RenderSwapChain = tempSwapChain.QueryInterface<SwapChain3>();
    }

    /// <summary>
    /// Создаём кучи для дескрипторов
    /// </summary>
    private void CreateRtvAndDsvDescriptorHeaps()
    {
        var rtvHeapDesc = new DescriptorHeapDescription
        {
            DescriptorCount = RtvDescriptorCount,
            Type = DescriptorHeapType.RenderTargetView,
        };
        RenderRtvHeap = RenderDevice.CreateDescriptorHeap(rtvHeapDesc);

        var dsvHeapDesc = new DescriptorHeapDescription
        {
            DescriptorCount = DsvDescriptorCount,
            Type = DescriptorHeapType.DepthStencilView,
        };
        RenderDsvHeap = RenderDevice.CreateDescriptorHeap(dsvHeapDesc);
    }

    protected override void OnResizeInternal()
    {
        base.OnResizeInternal();
        Debug.Assert(RenderDevice != null);
        Debug.Assert(RenderSwapChain != null);
        Debug.Assert(RenderDirectCmdListAlloc != null);

        // Перед тем, как что-то делать, синронизируемся
        FlushCommandQueue();

        RenderCommandList.Reset(RenderDirectCmdListAlloc, null);

        // Освобождаем всё, что сейчас пересоздадим
        foreach (var buffer in _swapChainBuffers)
            buffer?.Dispose();
        DepthStencilBuffer?.Dispose();

        RenderSwapChain.ResizeBuffers(
            SwapChainBufferCount,
            Width, Height,
            BackBufferFormat,
            SwapChainFlags.AllowModeSwitch);

        var rtvHeapHandle = RenderRtvHeap.CPUDescriptorHandleForHeapStart;
        for (var i = 0; i < SwapChainBufferCount; i++)
        {
            var backBuffer = RenderSwapChain.GetBackBuffer<Resource>(i);
            _swapChainBuffers[i] = backBuffer;
            RenderDevice.CreateRenderTargetView(backBuffer, null, rtvHeapHandle);
            rtvHeapHandle += RtvDescriptorSize;
        }

        var depthStencilDesc = new ResourceDescription
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = Width,
            Height = Height,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format.R24G8_Typeless,
            SampleDescription = new SampleDescription
            {
                Count = MsaaCount,
                Quality = MsaaQuality,
            },
            Layout = TextureLayout.Unknown,
            Flags = ResourceFlags.AllowDepthStencil,
        };
        var optClear = new ClearValue
        {
            Format = DepthStencilFormat,
            DepthStencil = new DepthStencilValue
            {
                Depth = 1.0f,
                Stencil = 0,
            },
        };
        DepthStencilBuffer = RenderDevice.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            depthStencilDesc,
            ResourceStates.Common,
            optClear);

        var depthStencilViewDesc = new DepthStencilViewDescription
        {
            Dimension = M4xMsaaState
                ? DepthStencilViewDimension.Texture2DMultisampled
                : DepthStencilViewDimension.Texture2D,
            Format = DepthStencilFormat,
        };
        // Create descriptor to mip level 0 of entire resource using a depth stencil format.
        var dsvHeapHandle = RenderDsvHeap.CPUDescriptorHandleForHeapStart;
        RenderDevice.CreateDepthStencilView(DepthStencilBuffer, depthStencilViewDesc, dsvHeapHandle);

        // Изменяем состояние ресурса: начальное -> для использования как буфер глубины.
        RenderCommandList.ResourceBarrierTransition(DepthStencilBuffer, ResourceStates.Common, ResourceStates.DepthWrite);

        RenderCommandList.Close();
        RenderCommandQueue.ExecuteCommandList(RenderCommandList);

        FlushCommandQueue();

        RenderViewport = new ViewportF(0, 0, Width, Height, 0.0f, 1.0f);
        ScissorRectangle = new RectangleF(0, 0, Width, Height);
    }

    /// <summary>
    /// Синхронизация путём ожидания завершения всех команд GPU в очереди исполнения
    /// </summary>
    protected void FlushCommandQueue()
    {
        // Увеличиваем значение барьера для отметки команд до этой точки барьера.
        CurrentFence++;

        // Добавляем инструкцию в очередь команд для установки новой точки барьера.
        // Поскольку мы работаем на временной линии GPU, новая точка барьера не будет
        // установлена до тех пор, пока GPU не завершит обработку всех команд до этого Signal().
        RenderCommandQueue.Signal(RenderFence, CurrentFence);

        // Ждем, пока GPU завершит выполнение команд до этой точки барьера.
        if (RenderFence.CompletedValue < CurrentFence)
        {
            // Вызываем событие, когда GPU достигнет текущего барьера.
            RenderFence.SetEventOnCompletion(CurrentFence, FenceEvent.SafeWaitHandle.DangerousGetHandle());

            // Ожидаем срабатывания события достижения текущего барьера GPU.
            FenceEvent.WaitOne();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FlushCommandQueue();

            RenderRtvHeap?.Dispose();
            RenderDsvHeap?.Dispose();
            RenderSwapChain?.Dispose();
            foreach (var buffer in _swapChainBuffers)
                 buffer?.Dispose();
            DepthStencilBuffer?.Dispose();
            RenderCommandList?.Dispose();
            RenderDirectCmdListAlloc?.Dispose();
            RenderCommandQueue?.Dispose();
            RenderFence?.Dispose();
            RenderDevice?.Dispose();
        }

        base.Dispose(disposing);
    }

    private int _frameCount;
    private float _timeElapsed;

    /// <summary>
    /// Считаем фпс
    /// </summary>
    private void CalculateFrameRateStats()
    {
        _frameCount++;

        if (Timer.TotalTime - _timeElapsed >= 1.0f)
        {
            float fps = _frameCount;
            var mspf = 1000.0f / fps;

            Window.Text = $"{WindowTitle}    fps: {fps}   mspf: {mspf}";

            _frameCount = 0;
            _timeElapsed += 1.0f;
        }
    }

    /// The Direct3D 12 debug layer may or may not be installed. It's installation can be
    /// managed through settings page "Manage optional features" with a feature called
    /// "Graphics Tools".
    private static void CheckDebugLayer()
    {

#if DEBUG
        try
        {
            DebugInterface.Get().EnableDebugLayer();
        }
        catch (SharpDXException ex) when (ex.Descriptor.NativeApiCode == "DXGI_ERROR_SDK_COMPONENT_MISSING")
        {
            Debug.WriteLine("Failed to enable debug layer. Please ensure \"Graphics Tools\" feature is enabled in Windows \"Manage optional feature\" settings page");
        }
#endif
    }
}
