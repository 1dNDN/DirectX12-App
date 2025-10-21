using System.Windows.Forms;

using RealGen.Utils;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

namespace RealGen;

public class DirectXApp : DirectXWindow
{
    /// <summary>
    /// Куча для дескрипторов Constant Buffer
    /// </summary>
    protected DescriptorHeap CbvHeap { get; set; }

    /// <summary>
    /// Список куч с дескрипторами буферов
    /// </summary>
    protected DescriptorHeap[] DescriptorHeaps { get; set; }

    /// <summary>
    /// Инстанс для обращения к constant buffer
    /// </summary>
    internal UploadBuffer<ObjectConstants> CurrentConstantBuffer { get; set; }

    /// <summary>
    /// Геометрия сцены
    /// </summary>
    protected MeshGeometry Geometry { get; set; }

    /// <summary>
    /// Описание функции шейдеров
    /// </summary>
    protected RootSignature RenderRootSignature { get; set; }

    /// <summary>
    /// Байткод скомпилированного вертексного шейдера
    /// </summary>
    private ShaderBytecode VertexShaderByteCode { get; set; }

    /// <summary>
    /// Байткод скомпилированного пиксельного шейдера
    /// </summary>
    private ShaderBytecode PixelShaderByteCode { get; set; }

    /// <summary>
    /// Состояние графического пайплайна
    /// </summary>
    private PipelineState PSO { get; set; }

    /// <summary>
    /// Описание входных аргументов шейдера (вертексного)
    /// </summary>
    private InputLayoutDescription ShaderInputLayout { get; set; }

    /// <summary>
    /// Матрица проекции
    /// </summary>
    private Matrix Proj { get; set; } = Matrix.Identity;

    /// <summary>
    /// Матрица камеры
    /// </summary>
    private Matrix View { get; set; } = Matrix.Identity;

    public override void Init()
    {
        base.Init();

        RenderCommandList.Reset(RenderDirectCmdListAlloc, null);

        BuildDescriptorHeaps();
        BuildConstantBuffers();
        BuildRootSignature();
        BuildShadersAndInputLayout();
        BuildGeometry();
        BuildPSO();

        RenderCommandList.Close();
        RenderCommandQueue.ExecuteCommandList(RenderCommandList);

        FlushCommandQueue();
    }

    protected override void Draw(GameTimer gameTimer)
    {

        // Reuse the memory associated with command recording.
        // We can only reset when the associated command lists have finished execution on the GPU.
        RenderDirectCmdListAlloc.Reset();

        // A command list can be reset after it has been added to the command queue via ExecuteCommandList.
        // Reusing the command list reuses memory.
        RenderCommandList.Reset(RenderDirectCmdListAlloc, PSO);

        // Set the viewport and scissor rect. This needs to be reset whenever the command list is reset.
        RenderCommandList.SetViewport(RenderViewport);
        RenderCommandList.SetScissorRectangles(ScissorRectangle);

        // Indicate a state transition on the resource usage.
        RenderCommandList.ResourceBarrierTransition(CurrentBackBuffer, ResourceStates.Present, ResourceStates.RenderTarget);

        // Clear the back buffer and depth buffer.
        RenderCommandList.ClearRenderTargetView(CurrentBackBufferView, Color.LightSteelBlue);
        RenderCommandList.ClearDepthStencilView(DepthStencilView, ClearFlags.FlagsDepth | ClearFlags.FlagsStencil, 1.0f, 0);

        // Specify the buffers we are going to render to.
        RenderCommandList.SetRenderTargets(CurrentBackBufferView, DepthStencilView);

        RenderCommandList.SetDescriptorHeaps(DescriptorHeaps.Length, DescriptorHeaps);

        RenderCommandList.SetGraphicsRootSignature(RenderRootSignature);

        RenderCommandList.SetVertexBuffer(0, Geometry.VertexBufferView);
        RenderCommandList.SetIndexBuffer(Geometry.IndexBufferView);
        RenderCommandList.PrimitiveTopology = PrimitiveTopology.TriangleList;

        RenderCommandList.SetGraphicsRootDescriptorTable(0, CbvHeap.GPUDescriptorHandleForHeapStart);

        RenderCommandList.DrawIndexedInstanced(Geometry.IndexCount, 1, 0, 0, 0);

        // Indicate a state transition on the resource usage.
        RenderCommandList.ResourceBarrierTransition(CurrentBackBuffer, ResourceStates.RenderTarget, ResourceStates.Present);

        // Done recording commands.
        RenderCommandList.Close();

        // Add the command list to the queue for execution.
        RenderCommandQueue.ExecuteCommandList(RenderCommandList);

        // Present the buffer to the screen. Presenting will automatically swap the back and front buffers.
        RenderSwapChain.Present(0, PresentFlags.None);

        // Wait until frame commands are complete. This waiting is inefficient and is
        // done for simplicity. Later we will show how to organize our rendering code
        // so we do not have to wait per frame.
        FlushCommandQueue();
    }

    /// <summary>
    /// Зенитный угол
    /// </summary>
    private float _theta = 1.5f * MathUtil.Pi;

    /// <summary>
    /// Азимутальный угол
    /// </summary>
    private float _phi = MathUtil.PiOverFour;

    /// <summary>
    ///  Расстояние от камеры до начала координат
    /// </summary>
    private float _radius = 5.0f;

    protected override void Update(GameTimer gt)
    {
        // Конвертация сферических координат к декартовым.
        var x = _radius * MathHelper.Sinf(_phi) * MathHelper.Cosf(_theta);
        var z = _radius * MathHelper.Sinf(_phi) * MathHelper.Sinf(_theta);
        var y = _radius * MathHelper.Cosf(_phi);

        // Вычисляем матрицу View
        View = Matrix.LookAtLH(new Vector3(x, y, z), Vector3.Zero, Vector3.Up);

        // Simply use identity for world matrix for this demo.
        var world = Matrix.Identity;

        var cb = new ObjectConstants
        {
            WorldViewProj = Matrix.Transpose(world * View * Proj),
        };

        CurrentConstantBuffer.CopyData(0, ref cb);
    }

    private Point _lastMousePos;

    protected override void OnMouseDown(MouseButtons button, Point location)
    {
        base.OnMouseDown(button, location);
        _lastMousePos = location;
    }

    protected override void OnMouseMove(MouseButtons button, Point location)
    {
        if ((button & MouseButtons.Left) != 0)
        {
            // Один пиксель - четверть градуса.
            var dx = MathUtil.DegreesToRadians(0.25f * (location.X - _lastMousePos.X));
            var dy = MathUtil.DegreesToRadians(0.25f * (location.Y - _lastMousePos.Y));

            _theta += dx;
            _phi += dy;

            // Ограничиваем зенитный угол
            _phi = MathUtil.Clamp(_phi, 0.1f, MathUtil.Pi - 0.1f);
        }
        else if ((button & MouseButtons.Right) != 0)
        {
            // Один пиксель - четверть градуса.
            var dx = 0.005f * (location.X - _lastMousePos.X);
            var dy = 0.005f * (location.Y - _lastMousePos.Y);

            _radius += dx - dy;

            // Ограничиваем радиус
            _radius = MathUtil.Clamp(_radius, 3.0f, 15.0f);
        }

        _lastMousePos = location;
    }

    protected override void OnResizeInternal()
    {
        base.OnResizeInternal();

        // The window resized, so update the aspect ratio and recompute the projection matrix.
        Proj = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, AspectRatio, 1.0f, 1000.0f);
    }

    /// <summary>
    /// Создаёт кучу с дескрипторами
    /// </summary>
    private void BuildDescriptorHeaps()
    {
        var cbvHeapDesc = new DescriptorHeapDescription
        {
            DescriptorCount = 1,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            Flags = DescriptorHeapFlags.ShaderVisible,
            NodeMask = 0,
        };
        CbvHeap = RenderDevice.CreateDescriptorHeap(cbvHeapDesc);
        DescriptorHeaps = [CbvHeap];
    }

    private void BuildConstantBuffers()
    {
        var sizeInBytes = BufferUtil.CalcConstantBufferByteSize<ObjectConstants>();

        CurrentConstantBuffer = new UploadBuffer<ObjectConstants>(RenderDevice, 1, true);

        var cbvDesc = new ConstantBufferViewDescription
        {
            BufferLocation = CurrentConstantBuffer.Resource.GPUVirtualAddress,
            SizeInBytes = sizeInBytes,
        };
        var cbvHeapHandle = CbvHeap.CPUDescriptorHandleForHeapStart;
        RenderDevice.CreateConstantBufferView(cbvDesc, cbvHeapHandle);
    }

    private void BuildRootSignature()
    {
        // Shader programs typically require resources as input (constant buffers,
        // textures, samplers). The root signature defines the resources the shader
        // programs expect. If we think of the shader programs as a function, and
        // the input resources as function parameters, then the root signature can be
        // thought of as defining the function signature.

        // Root parameter can be a table, root descriptor or root constants.

        // Create a single descriptor table of CBVs.
        var cbvTable = new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 0);

        // A root signature is an array of root parameters.
        var rootSigDesc = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, new[]
        {
            new RootParameter(ShaderVisibility.Vertex, cbvTable)
        });

        RenderRootSignature = RenderDevice.CreateRootSignature(rootSigDesc.Serialize());
    }


    private void BuildShadersAndInputLayout()
    {
        VertexShaderByteCode = ShaderUtil.CompileShader("Shaders/vertex.hlsl", "VS", "vs_5_0");
        PixelShaderByteCode = ShaderUtil.CompileShader("Shaders/pixel.hlsl", "PS", "ps_5_0");

        ShaderInputLayout = new InputLayoutDescription(
        [
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
        ]);
    }

    private void BuildGeometry()
    {
        Vertex[] vertices =
        [
            new Vertex { Pos = new Vector3(-1.0f, -1.0f, -1.0f), Color = Color.White.ToVector4() },
            new Vertex { Pos = new Vector3(-1.0f, +1.0f, -1.0f), Color = Color.Black.ToVector4() },
            new Vertex { Pos = new Vector3(+1.0f, +1.0f, -1.0f), Color = Color.Red.ToVector4() },
            new Vertex { Pos = new Vector3(+1.0f, -1.0f, -1.0f), Color = Color.Green.ToVector4() },
            new Vertex { Pos = new Vector3(-1.0f, -1.0f, +1.0f), Color = Color.Blue.ToVector4() },
            new Vertex { Pos = new Vector3(-1.0f, +1.0f, +1.0f), Color = Color.Yellow.ToVector4() },
            new Vertex { Pos = new Vector3(+1.0f, +1.0f, +1.0f), Color = Color.Cyan.ToVector4() },
            new Vertex { Pos = new Vector3(+1.0f, -1.0f, +1.0f), Color = Color.Magenta.ToVector4() },
        ];

        short[] indices =
        [
            // front face
            0, 1, 2,
            0, 2, 3,

            // back face
            4, 6, 5,
            4, 7, 6,

            // left face
            4, 5, 1,
            4, 1, 0,

            // right face
            3, 2, 6,
            3, 6, 7,

            // top face
            1, 5, 6,
            1, 6, 2,

            // bottom face
            4, 0, 3,
            4, 3, 7,
        ];

        Geometry = MeshGeometry.New(RenderDevice, RenderCommandList, vertices, indices);
    }

    private void BuildPSO()
    {
        var psoDesc = new GraphicsPipelineStateDescription
        {
            InputLayout = ShaderInputLayout,
            RootSignature = RenderRootSignature,
            VertexShader = VertexShaderByteCode,
            PixelShader = PixelShaderByteCode,
            RasterizerState = RasterizerStateDescription.Default(),
            BlendState = BlendStateDescription.Default(),
            DepthStencilState = DepthStencilStateDescription.Default(),
            SampleMask = int.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            RenderTargetCount = 1,
            SampleDescription = new SampleDescription(MsaaCount, MsaaQuality),
            DepthStencilFormat = DepthStencilFormat,
        };
        psoDesc.RenderTargetFormats[0] = BackBufferFormat;

        PSO = RenderDevice.CreateGraphicsPipelineState(psoDesc);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RenderRootSignature?.Dispose();
            CbvHeap?.Dispose();
            CurrentConstantBuffer?.Dispose();
            Geometry?.Dispose();
            PSO?.Dispose();
        }

        base.Dispose(disposing);
    }
}
