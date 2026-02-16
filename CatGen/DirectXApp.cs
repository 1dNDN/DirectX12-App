using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using CatGen.AssetServices;
using CatGen.Unifiers;
using CatGen.Utils;

using CatGen.DTOs;

using CatGen.Interfaces;
using CatGen.Saves;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

using Color = SharpDX.Color;
using Point = SharpDX.Point;
using ShaderResourceViewDimension = SharpDX.Direct3D12.ShaderResourceViewDimension;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace CatGen;

/// <summary>
/// Окошко, в котором всё делаем
/// </summary>
public class DirectXApp : BaseDirectXWindow, IRenderEngine
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public DirectXApp() : base()
    {
        Camera = new Camera(Timer);
    }

    /// <summary>
    /// Куча для дескрипторов Constant Buffer
    /// </summary>
    protected DescriptorHeap CbvHeap { get; set; }

    /// <summary>
    /// Куча для дескрипторов Shader Resource View
    /// </summary>
    protected DescriptorHeap SrvDescriptorHeap { get; set; }

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
    /// Байткод скомпилированного вертексного шейдера
    /// </summary>
    private ShaderBytecode VertexShaderByteCode { get; set; }

    /// <summary>
    /// Байткод скомпилированного пиксельного шейдера
    /// </summary>
    private ShaderBytecode PixelShaderByteCode { get; set; }

    /// <summary>
    /// Байткод скомпилированного пиксельного шейдера
    /// </summary>
    private ShaderBytecode PixelAlphatestdShaderByteCode { get; set; }

    /// <summary>
    /// Состояния графического пайплайна
    /// </summary>
    protected readonly Dictionary<PSOEnum, PipelineState> PSOs = new();



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
    protected PassConstants MainPassConstantBuffer = PassConstants.Default;

    /// <summary>
    /// Рисовать ли полигоны в виде сетки или как настоящие. True - в виде сетки, False - как настоящие
    /// </summary>
    protected bool IsWireframe = false;

    /// <summary>
    /// Камера
    /// </summary>
    protected Camera Camera { get; set; }

    /// <summary>
    /// Сцена с объектами и всей хернёй, что мы рендерим.
    /// </summary>
    protected SceneResourcesService Scene { get; set; }

    /// <summary>
    /// Редактор сцены, чтобы не пересобирать каждый раз всё заново
    /// </summary>
    protected EditorForm Editor = null!;

    private Thread _editorThread = null!;

    /// <inheritdoc />
    [MemberNotNull(nameof(Editor))]
    public override void Init()
    {
        base.Init();

        Scene = new SceneResourcesService(RenderDevice, RenderCommandList);

        Editor = new EditorForm(this);
        _editorThread = new Thread(() =>
        {
            Application.Run(new EditorForm(this));
        });

        _editorThread.IsBackground = true;
        _editorThread.SetApartmentState(ApartmentState.STA);
        _editorThread.Start();

        RenderCommandList.Reset(RenderDirectCmdListAlloc, null);

        Camera.Position = new Vector3(0.0f, 2.0f, -5.0f);

        BuildScene();
        BuildRootSignature();
        BuildDescriptorHeaps();
        BuildShadersAndInputLayout();
        BuildFrameResources();
        BuildPSOs();

        RenderCommandList.Close();
        RenderCommandQueue.ExecuteCommandList(RenderCommandList);

        FlushCommandQueue();
    }

    /// <inheritdoc />
    protected override void Draw(GameTimer gameTimer)
    {
        var cmdListAlloc = CurrentFrameResource.CmdListAlloc;

        // Reuse the memory associated with command recording.
        // We can only reset when the associated command lists have finished execution on the GPU.
        cmdListAlloc.Reset();

        // A command list can be reset after it has been added to the command queue via ExecuteCommandList.
        // Reusing the command list reuses memory.
        RenderCommandList.Reset(cmdListAlloc, IsWireframe ? PSOs[PSOEnum.OpaqueWireframe] : PSOs[PSOEnum.Opaque]);

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

        var passCb = CurrentFrameResource.PassConstantBuffer.Resource;
        RenderCommandList.SetGraphicsRootConstantBufferView(2, passCb.GPUVirtualAddress);

        DrawRenderItems(RenderCommandList, Scene.SceneItemLayers[RenderLayer.Opaque]);

        // TODO: переделать
        // RenderCommandList.PipelineState = PSOs[PSOEnum.AlphaTested];
        // DrawRenderItems(RenderCommandList, SceneItemLayers[RenderLayer.AlphaTested]);
        //
        // RenderCommandList.PipelineState = PSOs[PSOEnum.Transparent];
        // DrawRenderItems(RenderCommandList, SceneItemLayers[RenderLayer.Transparent]);

        // Indicate a state transition on the resource usage.
        RenderCommandList.ResourceBarrierTransition(CurrentBackBuffer, ResourceStates.RenderTarget, ResourceStates.Present);

        // Done recording commands.
        RenderCommandList.Close();

        // Add the command list to the queue for execution.
        RenderCommandQueue.ExecuteCommandList(RenderCommandList);

        // Present the buffer to the screen. Presenting will automatically swap the back and front buffers.
        RenderSwapChain.Present(0, PresentFlags.None);

        // Advance the fence value to mark commands up to this fence point.
        CurrentFrameResource.Fence = ++CurrentFence;

        // Add an instruction to the command queue to set a new fence point.
        // Because we are on the GPU timeline, the new fence point won't be
        // set until the GPU finishes processing all the commands prior to this Signal().
        RenderCommandQueue.Signal(RenderFence, CurrentFence);
        Scene.SceneLock.ExitReadLock();
    }

    /// <inheritdoc />
    protected override void Update(GameTimer timer)
    {
        if(!_editorThread.IsAlive)
            (Process.GetCurrentProcess()).Kill();

        Camera.Update();

        CurrentFrameIndex = (CurrentFrameIndex + 1) % NumFrameResources;

        // Has the GPU finished processing the commands of the current frame resource?
        // If not, wait until the GPU has completed commands up to this fence point.
        if (CurrentFrameResource.Fence != 0 && RenderFence.CompletedValue < CurrentFrameResource.Fence)
        {
            RenderFence.SetEventOnCompletion(CurrentFrameResource.Fence, CurrentFenceEvent.SafeWaitHandle.DangerousGetHandle());
            CurrentFenceEvent.WaitOne();
        }

        var dirty = Scene.Update(ResetCommandList);
        // TODO: убрать эту порнографию с делегатом

        Scene.SceneLock.EnterReadLock();
        if (dirty)
        {
            UpdateDescriptorHeaps();
            UpdateFrameResources();

            RenderCommandList.Close();
            RenderCommandQueue.ExecuteCommandList(RenderCommandList);

            FlushCommandQueue();
        }

        UpdateObjectCBs();
        UpdateMaterialCBs();
        UpdateMainPassCb(timer);
    }

    private void ResetCommandList()
    {
        FlushCommandQueue();

        RenderDirectCmdListAlloc.Reset();
        RenderCommandList.Reset(RenderDirectCmdListAlloc, null);
    }

    private void UpdateDescriptorHeaps()
    {
        SrvDescriptorHeap.Dispose();

        BuildDescriptorHeaps();
    }

    private void UpdateFrameResources()
    {
        foreach (var frameResource in Frames)
            frameResource.Dispose();

        Frames.Clear();
        FenceEvents.Clear();

        BuildFrameResources();
    }

    private void UpdateObjectCBs()
    {
        foreach (var e in Scene.SceneItems)
        {
            // Only update the cbuffer data if the constants have changed.
            // This needs to be tracked per frame resource.

            // Обновляем буфер констант только если константы изменились. Отслеживаем изменения для каждого кадра
            if (e.NumFramesDirty > 0)
            {
                var objConstants = new ObjectConstants
                {
                    World = Matrix.Transpose(e.World),
                    TexTransform = Matrix.Transpose(e.TexTransform)
                };

                CurrentFrameResource.ObjectConstantBuffer.CopyData(e.ObjCbIndex, ref objConstants);

                e.NumFramesDirty--;
            }
        }
    }

    private void UpdateMaterialCBs()
    {
        foreach (var mat in Scene.Materials.Values)
        {
            // Only update the cbuffer data if the constants have changed. If the cbuffer
            // data changes, it needs to be updated for each FrameResource.
            if (mat.NumFramesDirty > 0)
            {
                var matConstants = new MaterialConstants
                {
                    DiffuseAlbedo = mat.DiffuseAlbedo,
                    FresnelR0 = mat.FresnelR0,
                    Roughness = mat.Roughness,
                    MatTransform = Matrix.Transpose(mat.MatTransform),
                };

                CurrentFrameResource.MaterialConstantBuffer.CopyData(mat.MaterialCbIndex, ref matConstants);

                // Next FrameResource need to be updated too.
                mat.NumFramesDirty--;
            }
        }
    }

    private void UpdateMainPassCb(GameTimer timer)
    {
        var view = Camera.View;
        var proj = Camera.Proj;

        var viewProj = view * proj;
        var invView = Matrix.Invert(view);
        var invProj = Matrix.Invert(proj);
        var invViewProj = Matrix.Invert(viewProj);

        MainPassConstantBuffer.View = Matrix.Transpose(view);
        MainPassConstantBuffer.InvView = Matrix.Transpose(invView);
        MainPassConstantBuffer.Proj = Matrix.Transpose(proj);
        MainPassConstantBuffer.InvProj = Matrix.Transpose(invProj);
        MainPassConstantBuffer.ViewProj = Matrix.Transpose(viewProj);
        MainPassConstantBuffer.InvViewProj = Matrix.Transpose(invViewProj);
        MainPassConstantBuffer.EyePosW = Camera.Position;
        MainPassConstantBuffer.RenderTargetSize = new Vector2(Width, Height);
        MainPassConstantBuffer.InvRenderTargetSize = 1.0f / MainPassConstantBuffer.RenderTargetSize;
        MainPassConstantBuffer.NearZ = 1.0f;
        MainPassConstantBuffer.FarZ = 1000.0f;
        MainPassConstantBuffer.TotalTime = timer.TotalTime;
        MainPassConstantBuffer.DeltaTime = timer.DeltaTime;
        MainPassConstantBuffer.AmbientLight = new Vector4(0.25f, 0.25f, 0.35f, 1.0f);
        MainPassConstantBuffer.Lights[0].Direction = new Vector3(0.57735f, 0.57735f, 0.57735f);
        MainPassConstantBuffer.Lights[0].Strength = new Vector3(0.6f);
        MainPassConstantBuffer.Lights[1].Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f);
        MainPassConstantBuffer.Lights[1].Strength = new Vector3(0.2f);
        MainPassConstantBuffer.Lights[2].Direction = new Vector3(0.0f, -0.707f, -0.707f);
        MainPassConstantBuffer.Lights[2].Strength = new Vector3(0.15f);

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
            // Make each pixel correspond to a quarter of a degree.
            var dx = MathUtil.DegreesToRadians(0.25f * (location.X - _lastMousePos.X));
            var dy = MathUtil.DegreesToRadians(0.25f * (location.Y - _lastMousePos.Y));

            Camera.Pitch(dy);
            Camera.RotateY(dx);
        }

        _lastMousePos = location;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(Keys keyCode)
    {
        if (keyCode == Keys.D1)
            IsWireframe = true;
    }

    /// <inheritdoc />
    protected override void OnKeyUp(Keys keyCode)
    {
        base.OnKeyUp(keyCode);
        if (keyCode == Keys.D1)
            IsWireframe = false;
    }

    /// <inheritdoc />
    protected override void OnResizeInternal()
    {
        base.OnResizeInternal();

        // The window resized, so update the aspect ratio and recompute the projection matrix.
        Camera.SetLens(MathUtil.PiOverFour, AspectRatio, 1.0f, 1000.0f);
    }

    /// <summary>
    /// Создаёт кучу с дескрипторами
    /// </summary>
    private void BuildDescriptorHeaps()
    {
        var srvHeapDesc = new DescriptorHeapDescription
        {
            DescriptorCount = Scene.Textures.Count,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            Flags = DescriptorHeapFlags.ShaderVisible
        };

        SrvDescriptorHeap = RenderDevice.CreateDescriptorHeap(srvHeapDesc);
        DescriptorHeaps = [SrvDescriptorHeap];

        var descriptorHandle = SrvDescriptorHeap.CPUDescriptorHandleForHeapStart;
        var srvDescription = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = TextureUtil.DefaultShader4ComponentMapping,
            Dimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new ShaderResourceViewDescription.Texture2DResource
            {
                MostDetailedMip = 0,
                ResourceMinLODClamp = 0.0f,
            },
        };

        foreach (var texture in Scene.Textures.Values)
        {
            var resource = texture.Resource;
            srvDescription.Format = resource.Description.Format;
            srvDescription.Texture2D.MipLevels = resource.Description.MipLevels;

            RenderDevice.CreateShaderResourceView(resource, srvDescription, descriptorHandle);

            descriptorHandle += CbvSrvUavDescriptorSize;
        }

    }

    private void BuildScene()
    {
        Scene.AddModels(SaveService.GetModelsOnDisk());
        Scene.SpawnEntities(SaveService.GetSpawnedEntities());

        Scene.Load();
    }

    private void BuildRootSignature()
    {
        // Shader programs typically require resources as input (constant buffers,
        // textures, samplers). The root signature defines the resources the shader
        // programs expect. If we think of the shader programs as a function, and
        // the input resources as function parameters, then the root signature can be
        // thought of as defining the function signature.

        // Root parameter can be a table, root descriptor or root constants.

        // A root signature is an array of root parameters.
        var slotRootParameters = new[]
        {
            new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0)),
            new RootParameter(ShaderVisibility.All, new RootDescriptor(0, 0), RootParameterType.ConstantBufferView),
            new RootParameter(ShaderVisibility.All, new RootDescriptor(1, 0), RootParameterType.ConstantBufferView),
            new RootParameter(ShaderVisibility.All, new RootDescriptor(2, 0), RootParameterType.ConstantBufferView),
        };

        var rootSigDesc = new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters,
            GetStaticSamplers());

        RenderRootSignature = RenderDevice.CreateRootSignature(rootSigDesc.Serialize());
    }

    private StaticSamplerDescription[] GetStaticSamplers()
    {
        // Applications usually only need a handful of samplers. So just define them all up front
        // and keep them available as part of the root signature.

        return
        [
            // PointWrap
            new StaticSamplerDescription(ShaderVisibility.All, 0, 0)
            {
                Filter = Filter.MinMagMipPoint,
                AddressUVW = TextureAddressMode.Wrap
            },
            // PointClamp
            new StaticSamplerDescription(ShaderVisibility.All, 1, 0)
            {
                Filter = Filter.MinMagMipPoint,
                AddressUVW = TextureAddressMode.Clamp
            },
            // LinearWrap
            new StaticSamplerDescription(ShaderVisibility.All, 2, 0)
            {
                Filter = Filter.MinMagMipLinear,
                AddressUVW = TextureAddressMode.Wrap
            },
            // LinearClamp
            new StaticSamplerDescription(ShaderVisibility.All, 3, 0)
            {
                Filter = Filter.MinMagMipLinear,
                AddressUVW = TextureAddressMode.Clamp
            },
            // AnisotropicWrap
            new StaticSamplerDescription(ShaderVisibility.All, 4, 0)
            {
                Filter = Filter.Anisotropic,
                AddressUVW = TextureAddressMode.Wrap,
                MipLODBias = 0.0f,
                MaxAnisotropy = 8
            },
            // AnisotropicClamp
            new StaticSamplerDescription(ShaderVisibility.All, 5, 0)
            {
                Filter = Filter.Anisotropic,
                AddressUVW = TextureAddressMode.Clamp,
                MipLODBias = 0.0f,
                MaxAnisotropy = 8
            },
        ];
    }


    private void BuildShadersAndInputLayout()
    {
        // ShaderMacro[] defines = { new ShaderMacro("FOG", "0") };
        //
        // ShaderMacro[] alphaTestDefines = { new ShaderMacro("FOG", "0"), new ShaderMacro("ALPHA_TEST", "1") };

        VertexShaderByteCode = ShaderUtil.CompileShader("Shaders\\Default.hlsl", "VS", "vs_5_0");
        PixelShaderByteCode = ShaderUtil.CompileShader("Shaders\\Default.hlsl", "PS", "ps_5_0");
        // PixelAlphatestdShaderByteCode = ShaderUtil.CompileShader("Shaders\\Default.hlsl", "PS", "ps_5_0", alphaTestDefines);
        ShaderInputLayout = new InputLayoutDescription(
        [
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
        ]);
    }

    private void BuildFrameResources()
    {
        for (var i = 0; i < NumFrameResources; i++)
        {
            Frames.Add(new Frame(RenderDevice, 1, Scene.SceneItems.Count, Scene.Materials.Count));
            FenceEvents.Add(new AutoResetEvent(false));
        }
    }

    private void DrawRenderItems(GraphicsCommandList cmdList, List<RenderItem> ritems)
    {
        var objectCbSizeBytes = BufferUtil.CalcConstantBufferByteSize<ObjectConstants>();
        var materialCbSizeBytes = BufferUtil.CalcConstantBufferByteSize<ObjectConstants>();

        var objectCb = CurrentFrameResource.ObjectConstantBuffer.Resource;
        var materialCb = CurrentFrameResource.MaterialConstantBuffer.Resource;

        foreach (var item in ritems)
        {
            cmdList.SetVertexBuffer(0, item.Geo.VertexBufferView);
            cmdList.SetIndexBuffer(item.Geo.IndexBufferView);
            cmdList.PrimitiveTopology = item.PrimitiveType;

            var textureHandle = SrvDescriptorHeap.GPUDescriptorHandleForHeapStart + item.Mat.DiffuseSrvHeapIndex * CbvSrvUavDescriptorSize;

            var objectCbAddress = objectCb.GPUVirtualAddress + item.ObjCbIndex * objectCbSizeBytes;
            var materialCbAddress = materialCb.GPUVirtualAddress + item.Mat.MaterialCbIndex * materialCbSizeBytes;

            cmdList.SetGraphicsRootDescriptorTable(0, textureHandle);
            cmdList.SetGraphicsRootConstantBufferView(1, objectCbAddress);
            cmdList.SetGraphicsRootConstantBufferView(3, materialCbAddress);

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

        PSOs[PSOEnum.Opaque] = RenderDevice.CreateGraphicsPipelineState(opaquePsoDesc);

        var opaqueWireframePsoDesc = opaquePsoDesc.Copy();
        opaqueWireframePsoDesc.RasterizerState.FillMode = FillMode.Wireframe;

        PSOs[PSOEnum.OpaqueWireframe] = RenderDevice.CreateGraphicsPipelineState(opaqueWireframePsoDesc);

        // var transparentPsoDesc = opaquePsoDesc.Copy();
        //
        // var transparencyBlendDesc = new RenderTargetBlendDescription
        // {
        //     IsBlendEnabled = true,
        //     LogicOpEnable = false,
        //     SourceBlend = BlendOption.SourceAlpha,
        //     DestinationBlend = BlendOption.InverseSourceAlpha,
        //     BlendOperation = BlendOperation.Add,
        //     SourceAlphaBlend = BlendOption.One,
        //     DestinationAlphaBlend = BlendOption.Zero,
        //     AlphaBlendOperation = BlendOperation.Add,
        //     LogicOp = LogicOperation.Noop,
        //     RenderTargetWriteMask = ColorWriteMaskFlags.All
        // };
        //
        // transparentPsoDesc.BlendState.RenderTarget[0] = transparencyBlendDesc;
        //
        // PSOs[PSOEnum.Transparent] = RenderDevice.CreateGraphicsPipelineState(transparentPsoDesc);
        //
        // var alphaTestedPsoDesc = opaquePsoDesc.Copy();
        // alphaTestedPsoDesc.PixelShader = PixelAlphatestdShaderByteCode;
        // alphaTestedPsoDesc.RasterizerState.CullMode = CullMode.None;
        //
        // PSOs[PSOEnum.AlphaTested] = RenderDevice.CreateGraphicsPipelineState(alphaTestedPsoDesc);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        SrvDescriptorHeap?.Dispose();
        RenderRootSignature?.Dispose();
        CbvHeap?.Dispose();
        foreach (var frameResource in Frames)
            frameResource.Dispose();

        Scene.Dispose();

        foreach (var pso in PSOs.Values)
            pso.Dispose();

        GC.SuppressFinalize(this);

        base.Dispose();
    }

    /// <inheritdoc cref="SceneResourcesService.AddModel"/>
    public void AddModel(ModelOnDisk path)
    {
        Scene.AddModel(path);
    }

    /// <inheritdoc cref="SceneResourcesService.DeleteModel"/>
    public void DeleteModel(ModelOnDisk item)
    {
        Scene.DeleteModel(item);
    }

    /// <inheritdoc cref="SceneResourcesService.SpawnEntity"/>
    public void SpawnObject(SpawnedEntityMetadata spawnedObject)
    {
        Scene.SpawnEntity(spawnedObject);
    }

    /// <inheritdoc cref="SceneResourcesService.DespawnEntity"/>
    public void DespawnEntity(SpawnedEntityMetadata item)
    {
        Scene.DespawnEntity(item);
    }

    /// <inheritdoc cref="SceneResourcesService.EditEntity"/>
    public void EditEntity(SpawnedEntityMetadata item)
    {
        Scene.EditEntity(item);
    }
}
