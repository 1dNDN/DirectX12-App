using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows.Forms;

using RealGen.Utils;

using SharpDX;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

namespace RealGen;

public class DirectXApp : BaseDirectXWindow
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
    /// Список кадров (для тройной буферизации)
    /// </summary>
    protected readonly List<Frame> Frames = new(NumFrameResources);

    protected readonly List<AutoResetEvent> FenceEvents = new(NumFrameResources);

    private Frame CurrentFrameResource => Frames[CurrentFrameIndex];

    private AutoResetEvent CurrentFenceEvent => FenceEvents[CurrentFrameIndex];

    /// <summary>
    /// Индекс текущего кадра в списке кадров
    /// </summary>
    protected int CurrentFrameIndex;

    /// <summary>
    /// Геометрии для сцены
    /// </summary>
    protected readonly Dictionary<string, MeshGeometry> Geometries = new();

    /// <summary>
    /// Байткод скомпилированного вертексного шейдера
    /// </summary>
    private ShaderBytecode VertexShaderByteCode { get; set; }

    /// <summary>
    /// Байткод скомпилированного пиксельного шейдера
    /// </summary>
    private ShaderBytecode PixelShaderByteCode { get; set; }

    /// <summary>
    /// Состояния графического пайплайна
    /// </summary>
    protected readonly Dictionary<string, PipelineState> PSOs = new();

    /// <summary>
    /// Список всех объектов геометрии сцены
    /// </summary>
    protected readonly List<RenderItem> SceneItems = [];

    /// <summary>
    /// Список всех объектов геометрии сцены, поделенных по PSO
    /// </summary>
    protected readonly Dictionary<RenderLayer, List<RenderItem>> SceneItemLayers = new(1)
    {
        [RenderLayer.Opaque] = [],
    };

    /// <summary>
    /// Описание ресурсов для графического пайплайна
    /// </summary>
    protected RootSignature RenderRootSignature { get; set; }

    /// <summary>
    /// Описание входных аргументов шейдера (вертексного)
    /// </summary>
    protected InputLayoutDescription ShaderInputLayout { get; set; }

    /// <summary>
    /// Буфер констант, привязанных к кадру (например, матрица камеры), с которым мы непосредственно работаем
    /// </summary>
    protected PassConstants MainPassConstantBuffer;

    //TODO:
    protected int _passCbvOffset;

    /// <summary>
    /// Рисовать ли полигоны в виде сетки или как настоящие. True - в виде сетки, False - как настоящие
    /// </summary>
    protected bool IsWireframe = true;

    private Vector3 EyePosition;

    /// <summary>
    /// Матрица проекции
    /// </summary>
    protected Matrix Proj { get; set; } = Matrix.Identity;

    /// <summary>
    /// Матрица камеры
    /// </summary>
    protected Matrix View { get; set; } = Matrix.Identity;

    public override void Init()
    {
        base.Init();

        RenderCommandList.Reset(RenderDirectCmdListAlloc, null);

        BuildRootSignature();
        BuildShadersAndInputLayout();
        BuildShapesAndGeometry();
        BuildRenderItems();
        BuildFrameResources();
        BuildDescriptorHeaps();
        BuildConstantBufferViews();
        BuildPSOs();

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
        RenderCommandList.Reset(RenderDirectCmdListAlloc, IsWireframe ? PSOs["opaque_wireframe"] : PSOs["opaque"]);

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

        var passCbvIndex = _passCbvOffset + CurrentFrameIndex;
        var passCbvHandle = CbvHeap.GPUDescriptorHandleForHeapStart;
        passCbvHandle += passCbvIndex * CbvSrvUavDescriptorSize;
        RenderCommandList.SetGraphicsRootDescriptorTable(1, passCbvHandle);

        DrawRenderItems(RenderCommandList, SceneItemLayers[RenderLayer.Opaque]);

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

    protected override void Update(GameTimer timer)
    {
        UpdateCamera();

        CurrentFrameIndex = (CurrentFrameIndex + 1) % NumFrameResources;

        // Has the GPU finished processing the commands of the current frame resource?
        // If not, wait until the GPU has completed commands up to this fence point.
        if (CurrentFrameResource.Fence != 0 && RenderFence.CompletedValue < CurrentFrameResource.Fence)
        {
            RenderFence.SetEventOnCompletion(CurrentFrameResource.Fence, CurrentFenceEvent.SafeWaitHandle.DangerousGetHandle());
            CurrentFenceEvent.WaitOne();
        }

        UpdateObjectCBs();
        UpdateMainPassCB(timer);
    }

    private void UpdateCamera()
    {
        // Конвертация сферических координат к декартовым.
        var x = _radius * MathHelper.Sinf(_phi) * MathHelper.Cosf(_theta);
        var z = _radius * MathHelper.Sinf(_phi) * MathHelper.Sinf(_theta);
        var y = _radius * MathHelper.Cosf(_phi);

        // Вычисляем матрицу View
        View = Matrix.LookAtLH(new Vector3(x, y, z), Vector3.Zero, Vector3.Up);
    }

    private void UpdateObjectCBs()
    {
        foreach (var e in SceneItems)
        {
            // Only update the cbuffer data if the constants have changed.
            // This needs to be tracked per frame resource.

            // Обновляем буфер констант только если константы изменились. Отслеживаем изменения для каждого кадра
            if (e.NumFramesDirty > 0)
            {
                var objConstants = new ObjectConstants
                {
                    World = Matrix.Transpose(e.World),
                };
                CurrentFrameResource.ObjectConstantBuffer.CopyData(e.ObjCBIndex, ref objConstants);

                e.NumFramesDirty--;
            }
        }
    }

    private void UpdateMainPassCB(GameTimer timer)
    {
        var viewProj = View * Proj;
        var invView = Matrix.Invert(View);
        var invProj = Matrix.Invert(Proj);
        var invViewProj = Matrix.Invert(viewProj);

        MainPassConstantBuffer.View = Matrix.Transpose(View);
        MainPassConstantBuffer.InvView = Matrix.Transpose(invView);
        MainPassConstantBuffer.Proj = Matrix.Transpose(Proj);
        MainPassConstantBuffer.InvProj = Matrix.Transpose(invProj);
        MainPassConstantBuffer.ViewProj = Matrix.Transpose(viewProj);
        MainPassConstantBuffer.InvViewProj = Matrix.Transpose(invViewProj);
        MainPassConstantBuffer.EyePosW = EyePosition;
        MainPassConstantBuffer.RenderTargetSize = new Vector2(Width, Height);
        MainPassConstantBuffer.InvRenderTargetSize = 1.0f / MainPassConstantBuffer.RenderTargetSize;
        MainPassConstantBuffer.NearZ = 1.0f;
        MainPassConstantBuffer.FarZ = 1000.0f;
        MainPassConstantBuffer.TotalTime = timer.TotalTime;
        MainPassConstantBuffer.DeltaTime = timer.DeltaTime;

        CurrentFrameResource.PassConstantBuffer.CopyData(0, ref MainPassConstantBuffer);
    }

    private Point _lastMousePos;

    /// <inheritdoc />
    protected override void OnMouseDown(MouseButtons button, Point location)
    {
        base.OnMouseDown(button, location);
        _lastMousePos = location;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override void OnKeyDown(Keys keyCode)
    {
        if (keyCode == Keys.D1)
            IsWireframe = false;
    }

    /// <inheritdoc />
    protected override void OnKeyUp(Keys keyCode)
    {
        base.OnKeyUp(keyCode);
        if (keyCode == Keys.D1)
            IsWireframe = true;
    }

    /// <inheritdoc />
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
        int objCount = SceneItems.Count;

        // Need a CBV descriptor for each object for each frame resource,
        // +1 for the perPass CBV for each frame resource.
        int numDescriptors = (objCount + 1) * NumFrameResources;

        // Save an offset to the start of the pass CBVs.  These are the last 3 descriptors.
        _passCbvOffset = objCount * NumFrameResources;

        var cbvHeapDesc = new DescriptorHeapDescription
        {
            DescriptorCount = numDescriptors,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            Flags = DescriptorHeapFlags.ShaderVisible,
            NodeMask = 0,
        };
        CbvHeap = RenderDevice.CreateDescriptorHeap(cbvHeapDesc);
        DescriptorHeaps = [CbvHeap];
    }

    private void BuildConstantBufferViews()
    {
        var objectConstantBufferSizeInBytes = BufferUtil.CalcConstantBufferByteSize<ObjectConstants>();

        for (var frameIndex = 0; frameIndex < NumFrameResources; frameIndex++)
        {
            var objectConstantBuffer = Frames[frameIndex].ObjectConstantBuffer.Resource;

            for (var i = 0; i < SceneItems.Count; i++)
            {
                var cbAddress = objectConstantBuffer.GPUVirtualAddress;

                // Offset to the ith object constant buffer in the buffer.
                cbAddress += i * objectConstantBufferSizeInBytes;

                // Offset to the object cbv in the descriptor heap.
                var heapIndex = frameIndex * SceneItems.Count + i;
                var handle = CbvHeap.CPUDescriptorHandleForHeapStart;
                handle += heapIndex * CbvSrvUavDescriptorSize;

                var cbvDesc = new ConstantBufferViewDescription
                {
                    BufferLocation = cbAddress,
                    SizeInBytes = objectConstantBufferSizeInBytes
                };

                RenderDevice.CreateConstantBufferView(cbvDesc, handle);

            }
        }

        var passConstantBufferByteSize = BufferUtil.CalcConstantBufferByteSize<PassConstants>();

        // Last three descriptors are the pass CBVs for each frame resource.
        for (var frameIndex = 0; frameIndex < NumFrameResources; frameIndex++)
        {
            var passConstantBuffer = Frames[frameIndex].PassConstantBuffer.Resource;
            var cbAddress = passConstantBuffer.GPUVirtualAddress;

            // Offset to the pass cbv in the descriptor heap.
            var heapIndex = _passCbvOffset + frameIndex;
            var handle = CbvHeap.CPUDescriptorHandleForHeapStart;
            handle += heapIndex * CbvSrvUavDescriptorSize;

            var cbvDesc = new ConstantBufferViewDescription
            {
                BufferLocation = cbAddress,
                SizeInBytes = passConstantBufferByteSize
            };

            RenderDevice.CreateConstantBufferView(cbvDesc, handle);
        }
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
        var cbvTable0 = new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 0);
        var cbvTable1 = new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 1);

        // A root signature is an array of root parameters.
        var slotRootParameters = new[]
        {
            new RootParameter(ShaderVisibility.Vertex, cbvTable0),
            new RootParameter(ShaderVisibility.Vertex, cbvTable1)
        };

        var rootSigDesc = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, slotRootParameters);

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

    private void BuildShapesAndGeometry()
    {
        var vertices = new List<SmallaVertex>();
        var indices = new List<short>();

        //TODO:
        // SubmeshGeometry box = AppendMeshData(GeometryGenerator.CreateBox(1.5f, 0.5f, 1.5f, 3), Color.DarkGreen, vertices, indices);
        // SubmeshGeometry grid = AppendMeshData(GeometryGenerator.CreateGrid(20.0f, 30.0f, 60, 40), Color.ForestGreen, vertices, indices);
        var sphere = GeometryGenerator.AppendMeshData(GeometryGenerator.CreateSphere(0.5f, 20, 20), Color.DarkRed, vertices, indices);
        var cylinder = GeometryGenerator.AppendMeshData(GeometryGenerator.CreateCylinder(0.5f, 0.5f, 3.0f, 20, 20), Color.SteelBlue, vertices, indices);


        var geo = MeshGeometry.New(RenderDevice, RenderCommandList, vertices, indices.ToArray(), "baseGeometry");

        // geo.DrawArgs["box"] = box;
        // geo.DrawArgs["grid"] = grid;
        geo.DrawArgs["sphere"] = sphere;
        geo.DrawArgs["cylinder"] = cylinder;

        Geometries[geo.Name] = geo;
    }

    private void BuildFrameResources()
    {
        for (var i = 0; i < NumFrameResources; i++)
        {
            Frames.Add(new Frame(RenderDevice, 1, SceneItems.Count));
            FenceEvents.Add(new AutoResetEvent(false));
        }
    }

    [SuppressMessage("ReSharper", "RedundantAssignment")]
    private void BuildRenderItems()
    {
        var itemIndex = 0;
        var huiTranslation = Matrix.Translation(-1.0F, -1.5F, 0.0F);

        AddRenderItem(RenderLayer.Opaque, itemIndex++, "baseGeometry", "sphere", Matrix.Translation(0.0f, 0.5f, 0.0f) * huiTranslation);
        AddRenderItem(RenderLayer.Opaque, itemIndex++, "baseGeometry", "sphere", Matrix.Translation(2.0f, 0.5f, 0.0f) * huiTranslation);
        AddRenderItem(RenderLayer.Opaque, itemIndex++, "baseGeometry", "cylinder", Matrix.Translation(1.0f, 1.5f, 0.0f) * huiTranslation);
        AddRenderItem(RenderLayer.Opaque, itemIndex++, "baseGeometry", "sphere", Matrix.Translation(1.0f, 3.0f, 0.0f) * huiTranslation);
    }

    private void AddRenderItem(RenderLayer layer, int objConstantBufferIndex, string geoName, string submeshName, Matrix? world = null)
    {
        var geo = Geometries[geoName];
        var submesh = geo.DrawArgs[submeshName];
        var renderItem = new RenderItem
        {
            ObjCBIndex = objConstantBufferIndex,
            Geo = geo,
            IndexCount = submesh.IndexCount,
            StartIndexLocation = submesh.StartIndexLocation,
            BaseVertexLocation = submesh.BaseVertexLocation,
            World = world ?? Matrix.Identity,
        };
        SceneItemLayers[layer].Add(renderItem);
        SceneItems.Add(renderItem);
    }

    private void DrawRenderItems(GraphicsCommandList cmdList, List<RenderItem> ritems)
    {
        foreach (var item in ritems)
        {
            cmdList.SetVertexBuffer(0, item.Geo.VertexBufferView);
            cmdList.SetIndexBuffer(item.Geo.IndexBufferView);
            cmdList.PrimitiveTopology = item.PrimitiveType;

            // Offset to the CBV in the descriptor heap for this object and for this frame resource.
            var cbvIndex = CurrentFrameIndex * SceneItems.Count + item.ObjCBIndex;
            var cbvHandle = CbvHeap.GPUDescriptorHandleForHeapStart;
            cbvHandle += cbvIndex * CbvSrvUavDescriptorSize;

            cmdList.SetGraphicsRootDescriptorTable(0, cbvHandle);

            cmdList.DrawIndexedInstanced(item.IndexCount, 1, item.StartIndexLocation, item.BaseVertexLocation, 0);
        }
    }

    private void BuildPSOs()
    {
        var opaquePsoDesc = new GraphicsPipelineStateDescription
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
        opaquePsoDesc.RenderTargetFormats[0] = BackBufferFormat;

        PSOs["opaque"] = RenderDevice.CreateGraphicsPipelineState(opaquePsoDesc);

        var opaqueWireframePsoDesc = opaquePsoDesc;
        opaqueWireframePsoDesc.RasterizerState.FillMode = FillMode.Wireframe;

        PSOs["opaque_wireframe"] = RenderDevice.CreateGraphicsPipelineState(opaqueWireframePsoDesc);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RenderRootSignature?.Dispose();
            CbvHeap?.Dispose();
            foreach (var frameResource in Frames)
                frameResource.Dispose();
            foreach (var geometry in Geometries.Values)
                geometry.Dispose();
            foreach (var pso in PSOs.Values)
                pso.Dispose();
        }

        base.Dispose(disposing);
    }
}
