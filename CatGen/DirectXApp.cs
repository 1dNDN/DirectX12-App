using System.Diagnostics.CodeAnalysis;

using CatGen.AssetServices;
using CatGen.Unifiers;
using CatGen.Utils;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D12;
using SharpDX.DXGI;

using Color = SharpDX.Color;
using Point = SharpDX.Point;
using ShaderResourceViewDimension = SharpDX.Direct3D12.ShaderResourceViewDimension;

namespace CatGen;

/// <summary>
/// Окошко, в котором всё делаем
/// </summary>
public class DirectXApp : BaseDirectXWindow
{
    /// <summary>
    /// Куча для дескрипторов Constant Buffer
    /// </summary>
    protected DescriptorHeap CbvHeap { get; set; }

    /// <summary>
    /// Куча для дескрипторов Shader Resource View
    /// </summary>
    protected DescriptorHeap SRVDescriptorHeap { get; set; }

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
    protected readonly Dictionary<GeometryEnum, MeshGeometry> Geometries = new();

    /// <summary>
    /// Материалы для геометрий сцены
    /// </summary>
    private Dictionary<MaterialsEnum, Material> Materials { get; set; } = new();

    /// <summary>
    /// Текстуры для геометрий сцены
    /// </summary>
    private readonly Dictionary<TexturesEnum, Texture> Textures = new();

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
    protected PassConstants MainPassConstantBuffer = PassConstants.Default;

    /// <summary>
    /// Рисовать ли полигоны в виде сетки или как настоящие. True - в виде сетки, False - как настоящие
    /// </summary>
    protected bool IsWireframe = false;

    /// <summary>
    /// Камера
    /// </summary>
    protected readonly Camera Camera = new();

    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        RenderCommandList.Reset(RenderDirectCmdListAlloc, null);
        Camera.Position = new Vector3(0.0f, 2.0f, -5.0f);

        BuildTextures();
        BuildRootSignature();
        BuildDescriptorHeaps();
        BuildShadersAndInputLayout();
        BuildShapesAndGeometry();
        BuildMaterials();
        BuildRenderItems();
        BuildFrameResources();
        // BuildConstantBufferViews();
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

        var passCB = CurrentFrameResource.PassConstantBuffer.Resource;
        RenderCommandList.SetGraphicsRootConstantBufferView(2, passCB.GPUVirtualAddress);

        DrawRenderItems(RenderCommandList, SceneItemLayers[RenderLayer.Opaque]);

        RenderCommandList.PipelineState = PSOs[PSOEnum.AlphaTested];
        DrawRenderItems(RenderCommandList, SceneItemLayers[RenderLayer.AlphaTested]);

        RenderCommandList.PipelineState = PSOs[PSOEnum.Transparent];
        DrawRenderItems(RenderCommandList, SceneItemLayers[RenderLayer.Transparent]);

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
    }

    /// <inheritdoc />
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
        UpdateMaterialCBs();
        UpdateMainPassCB(timer);
    }

    private void UpdateCamera()
    {
        var dx = 10.0f;
        var dt = Timer.DeltaTime;

        if (KeyboardUtil.IsKeyDown(Keys.LControlKey))
            dx *= 3.0f;
        if (KeyboardUtil.IsKeyDown(Keys.W))
            Camera.Walk(dx * dt);
        if (KeyboardUtil.IsKeyDown(Keys.S))
            Camera.Walk(-dx * dt);
        if (KeyboardUtil.IsKeyDown(Keys.A))
            Camera.Strafe(-dx * dt);
        if (KeyboardUtil.IsKeyDown(Keys.D))
            Camera.Strafe(dx * dt);
        if (KeyboardUtil.IsKeyDown(Keys.Space))
            Camera.MoveUp(dx * dt);
        if (KeyboardUtil.IsKeyDown(Keys.LShiftKey))
            Camera.MoveUp(-dx * dt);

        Camera.UpdateViewMatrix();
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
                    TexTransform = Matrix.Transpose(e.TexTransform)
                };
                CurrentFrameResource.ObjectConstantBuffer.CopyData(e.ObjCBIndex, ref objConstants);

                e.NumFramesDirty--;
            }
        }
    }

    private void UpdateMaterialCBs()
    {
        foreach (var mat in Materials.Values)
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

                CurrentFrameResource.MaterialConstantBuffer.CopyData(mat.MaterialCBIndex, ref matConstants);

                // Next FrameResource need to be updated too.
                mat.NumFramesDirty--;
            }
        }
    }

    private void UpdateMainPassCB(GameTimer timer)
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
        //TODO
        var srvHeapDesc = new DescriptorHeapDescription
        {
            DescriptorCount = Textures.Count,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            Flags = DescriptorHeapFlags.ShaderVisible
        };
        SRVDescriptorHeap = RenderDevice.CreateDescriptorHeap(srvHeapDesc);
        DescriptorHeaps = [SRVDescriptorHeap];

        var descriptorHandle = SRVDescriptorHeap.CPUDescriptorHandleForHeapStart;
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

        foreach (var texture in Textures.Values)
        {
            var resource = texture.Resource;
            srvDescription.Format = resource.Description.Format;
            srvDescription.Texture2D.MipLevels = resource.Description.MipLevels;

            RenderDevice.CreateShaderResourceView(resource, srvDescription, descriptorHandle);

            descriptorHandle += CbvSrvUavDescriptorSize;
        }

    }

    // private void BuildConstantBufferViews()
    // {
    //     var objectConstantBufferSizeInBytes = BufferUtil.CalcConstantBufferByteSize<ObjectConstants>();
    //
    //     for (var frameIndex = 0; frameIndex < NumFrameResources; frameIndex++)
    //     {
    //         var objectConstantBuffer = Frames[frameIndex].ObjectConstantBuffer.Resource;
    //
    //         for (var i = 0; i < SceneItems.Count; i++)
    //         {
    //             var cbAddress = objectConstantBuffer.GPUVirtualAddress;
    //
    //             // Offset to the ith object constant buffer in the buffer.
    //             cbAddress += i * objectConstantBufferSizeInBytes;
    //
    //             // Offset to the object cbv in the descriptor heap.
    //             var heapIndex = frameIndex * SceneItems.Count + i;
    //             var handle = CbvHeap.CPUDescriptorHandleForHeapStart;
    //             handle += heapIndex * CbvSrvUavDescriptorSize;
    //
    //             var cbvDesc = new ConstantBufferViewDescription
    //             {
    //                 BufferLocation = cbAddress,
    //                 SizeInBytes = objectConstantBufferSizeInBytes
    //             };
    //
    //             RenderDevice.CreateConstantBufferView(cbvDesc, handle);
    //
    //         }
    //     }
    //
    //     var passConstantBufferByteSize = BufferUtil.CalcConstantBufferByteSize<PassConstants>();
    //
    //     // Last three descriptors are the pass CBVs for each frame resource.
    //     for (var frameIndex = 0; frameIndex < NumFrameResources; frameIndex++)
    //     {
    //         var passConstantBuffer = Frames[frameIndex].PassConstantBuffer.Resource;
    //         var cbAddress = passConstantBuffer.GPUVirtualAddress;
    //
    //         // Offset to the pass cbv in the descriptor heap.
    //         var heapIndex = _passCbvOffset + frameIndex;
    //         var handle = CbvHeap.CPUDescriptorHandleForHeapStart;
    //         handle += heapIndex * CbvSrvUavDescriptorSize;
    //
    //         var cbvDesc = new ConstantBufferViewDescription
    //         {
    //             BufferLocation = cbAddress,
    //             SizeInBytes = passConstantBufferByteSize
    //         };
    //
    //         RenderDevice.CreateConstantBufferView(cbvDesc, handle);
    //     }
    // }

    private void BuildTextures()
    {
        AddGltfTexture(TexturesEnum.Duck, "./Models/Duck/glTF-Embedded/Duck.gltf");
        AddDdsTexture(TexturesEnum.Semitransparent, "./Models/DDSTextures/water1.dds");
        AddDdsTexture(TexturesEnum.Fence, "./Models/DDSTextures/WireFence.dds");
    }

    void AddGltfTexture(TexturesEnum name, string path)
    {
        var (img, sampler) = GLTFReader.ImportTexture(path);
        var texture = new Texture
        {
            Resource = TextureUtil.CreateTextureFromPNG(this.RenderDevice, img),
        };

        Textures[name] = texture;
    }

    private void AddDdsTexture(TexturesEnum name, string path)
    {
        var texture = new Texture
        {
            Resource = DDSReader.ImportTexture(this.RenderDevice, path),
        };

        Textures[name] = texture;
    }

    private void BuildRootSignature()
    {
        // Shader programs typically require resources as input (constant buffers,
        // textures, samplers). The root signature defines the resources the shader
        // programs expect. If we think of the shader programs as a function, and
        // the input resources as function parameters, then the root signature can be
        // thought of as defining the function signature.

        // Root parameter can be a table, root descriptor or root constants.

        var textureTable = new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0);

        // Create a single descriptor table of CBVs.
        var descriptor1 = new RootDescriptor(0, 0);
        var descriptor2 = new RootDescriptor(1, 0);
        var descriptor3 = new RootDescriptor(2, 0);

        // A root signature is an array of root parameters.
        var slotRootParameters = new[]
        {
            new RootParameter(ShaderVisibility.Pixel, textureTable),
            new RootParameter(ShaderVisibility.Vertex, descriptor1, RootParameterType.ConstantBufferView),
            new RootParameter(ShaderVisibility.All, descriptor2, RootParameterType.ConstantBufferView),
            new RootParameter(ShaderVisibility.All, descriptor3, RootParameterType.ConstantBufferView)
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
        ShaderMacro[] defines =
        {
            new ShaderMacro("FOG", "0")
        };

        ShaderMacro[] alphaTestDefines =
        {
            new ShaderMacro("FOG", "0"),
            new ShaderMacro("ALPHA_TEST", "1")
        };

        VertexShaderByteCode = ShaderUtil.CompileShader("Shaders\\Default.hlsl", "VS", "vs_5_0");
        PixelShaderByteCode = ShaderUtil.CompileShader("Shaders\\Default.hlsl", "PS", "ps_5_0", defines);
        PixelAlphatestdShaderByteCode = ShaderUtil.CompileShader("Shaders\\Default.hlsl", "PS", "ps_5_0", alphaTestDefines);

        ShaderInputLayout = new InputLayoutDescription(new[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0)
        });
    }

    private void BuildShapesAndGeometry()
    {
        var vertices = new List<BiggaVertex>();
        var indices = new List<int>();

        var box = GeometryGenerator.AppendMeshData(GeometryGenerator.CreateBox(1.5f, 0.5f, 1.5f, 3), vertices, indices);
        var grid = GeometryGenerator.AppendMeshData(GeometryGenerator.CreateGrid(20.0f, 30.0f, 60, 40), vertices, indices);
        var sphere = GeometryGenerator.AppendMeshData(GeometryGenerator.CreateSphere(0.5f, 20, 20), vertices, indices);
        var cylinder = GeometryGenerator.AppendMeshData(GeometryGenerator.CreateCylinder(0.5f, 0.5f, 3.0f, 20, 20), vertices, indices);
        var jenjina = GeometryGenerator.AppendMeshData(OBJReader.Import("./Models/jenjina.obj"), vertices, indices);
        var superbox = GeometryGenerator.AppendMeshData(GLTFReader.ImportGeometry("./Models/BoxTextured/glTF-Embedded/BoxTextured.gltf"), vertices, indices);
        var duck = GeometryGenerator.AppendMeshData(GLTFReader.ImportGeometry("./Models/Duck/glTF-Embedded/Duck.gltf"), vertices, indices);

        var bigDragon = GeometryGenerator.AppendMeshData(STLReader.Import("./Models/big_dragon.stl"), vertices, indices);

        var geo = MeshGeometry.New(RenderDevice, RenderCommandList, vertices, indices.ToArray(), GeometryEnum.Duck);

        geo.DrawArgs[MeshEnum.Box] = box;
        geo.DrawArgs[MeshEnum.Grid] = grid;
        geo.DrawArgs[MeshEnum.Sphere] = sphere;
        geo.DrawArgs[MeshEnum.Cylinder] = cylinder;
        geo.DrawArgs[MeshEnum.Jenjina] = jenjina;
        geo.DrawArgs[MeshEnum.Superbox] = superbox;
        geo.DrawArgs[MeshEnum.Duck] = duck;
        geo.DrawArgs[MeshEnum.BigDragon] = bigDragon;

        Geometries[geo.Name] = geo;

        var geo2 = MeshGeometry.New(RenderDevice, RenderCommandList, vertices, indices.ToArray(), GeometryEnum.Fence);

        geo2.DrawArgs[MeshEnum.Box] = box;
        Geometries[geo2.Name] = geo2;

        var geo3 = MeshGeometry.New(RenderDevice, RenderCommandList, vertices, indices.ToArray(), GeometryEnum.Semitransparent);

        geo3.DrawArgs[MeshEnum.Grid] = grid;
        Geometries[geo3.Name] = geo3;
    }

    private void BuildFrameResources()
    {
        for (var i = 0; i < NumFrameResources; i++)
        {
            Frames.Add(new Frame(RenderDevice, 1, SceneItems.Count, Materials.Count));
            FenceEvents.Add(new AutoResetEvent(false));
        }
    }

    [SuppressMessage("ReSharper", "RedundantAssignment")]
    private void BuildMaterials()
    {
        // AddMaterial(new Material
        // {
        //     Name = "bricks0",
        //     MaterialCBIndex = 0,
        //     DiffuseSrvHeapIndex = 0,
        //     DiffuseAlbedo = Color.ForestGreen.ToVector4(),
        //     FresnelR0 = new Vector3(0.02f),
        //     Roughness = 0.1f
        // });
        // AddMaterial(new Material
        // {
        //     Name = "stone0",
        //     MaterialCBIndex = 1,
        //     DiffuseSrvHeapIndex = 1,
        //     DiffuseAlbedo = Color.LightSteelBlue.ToVector4(),
        //     FresnelR0 = new Vector3(0.05f),
        //     Roughness = 0.3f
        // });
        // AddMaterial(new Material
        // {
        //     Name = "tile0",
        //     MaterialCBIndex = 2,
        //     DiffuseSrvHeapIndex = 2,
        //     DiffuseAlbedo = Color.LightGray.ToVector4(),
        //     FresnelR0 = new Vector3(0.02f),
        //     Roughness = 0.2f
        // });
        // AddMaterial(new Material
        // {
        //     Name = "jenjinaMat",
        //     MaterialCBIndex = 3,
        //     DiffuseSrvHeapIndex = 3,
        //     DiffuseAlbedo = Color.BlueViolet.ToVector4(),
        //     FresnelR0 = new Vector3(0.05f),
        //     Roughness = 0.3f
        // });
        // AddMaterial(new Material
        // {
        //     Name = "superbox",
        //     MaterialCBIndex = 4,
        //     DiffuseSrvHeapIndex = 4,
        //     DiffuseAlbedo = Color.PaleGreen.ToVector4(),
        //     FresnelR0 = new Vector3(0.5f),
        //     Roughness = 0.9f
        // });
        // AddMaterial(new Material
        // {
        //     Name = "big_dragon",
        //     MaterialCBIndex = 5,
        //     DiffuseSrvHeapIndex = 5,
        //     DiffuseAlbedo = Color.PaleGreen.ToVector4(),
        //     FresnelR0 = new Vector3(0.5f),
        //     Roughness = 0.9f
        // });

        var materialIndex = 0;
        AddMaterial(
            new Material
            {
                Name = MaterialsEnum.Duck,
                DiffuseAlbedo = Color.White.ToVector4(),
                FresnelR0 = new Vector3(0.05f),
                Roughness = 0.2f,
            },
            ref materialIndex);

        AddMaterial(
            new Material
            {
                Name = MaterialsEnum.Semitransparent,
                DiffuseAlbedo = new Vector4(1.0f, 1.0f, 1.0f, 0.5f),
                FresnelR0 = new Vector3(0.1f),
                Roughness = 0.0f,
            },
            ref materialIndex);

        AddMaterial(
            new Material
            {
                Name = MaterialsEnum.Fence,
                DiffuseAlbedo = new Vector4(1.0f),
                FresnelR0 = new Vector3(0.1f),
                Roughness = 0.25f,
            },
            ref materialIndex);
    }

    private void AddMaterial(Material mat, ref int materialIndex)
    {
        mat.MaterialCBIndex = materialIndex;
        mat.DiffuseSrvHeapIndex = materialIndex;
        Materials[mat.Name] = mat;

        materialIndex++;
    }


    [SuppressMessage("ReSharper", "RedundantAssignment")]
    private void BuildRenderItems()
    {
        var itemIndex = 0;

        AddRenderItem(
            RenderLayer.Opaque,
            ref itemIndex,
            MaterialsEnum.Duck,
            GeometryEnum.Duck,
            MeshEnum.Duck,
            Matrix.Translation(0.0f, 0.0f, 0.0f) *
            Matrix.Scaling(1.0F) *
            Matrix.RotationYawPitchRoll(0.0F, -float.Pi/2, float.Pi));

        AddRenderItem(
            RenderLayer.Transparent,
            ref itemIndex,
            MaterialsEnum.Semitransparent,
            GeometryEnum.Semitransparent,
            MeshEnum.Grid,
            Matrix.Translation(0.0F, 0.0F, 0.0F));

        AddRenderItem(
            RenderLayer.AlphaTested,
            ref itemIndex,
            MaterialsEnum.Fence,
            GeometryEnum.Fence,
            MeshEnum.Box,
            Matrix.Translation(-3.0F, 0.0F, 0.0F));

        // AddRenderItem(RenderLayer.Opaque, itemIndex++, "jenjinaMat", "baseGeometry", "jenjina",
        //     Matrix.Translation(0.0f, 0.0f, 0.0f) *
        //     Matrix.Scaling(4.0F) *
        //     Matrix.RotationYawPitchRoll(0.0F, -float.Pi/2, float.Pi));

        // AddRenderItem(RenderLayer.Opaque, itemIndex++, "big_dragon", "baseGeometry", "big_dragon",
        //     Matrix.Translation(0.0f, 0.0f, 0.0f) *
        //     Matrix.Scaling(2.0F) *
        //     Matrix.RotationYawPitchRoll(0.0F, -float.Pi/2, float.Pi));

        // AddRenderItem(RenderLayer.Opaque, itemIndex++, "baseGeometry", "sphere", Matrix.Translation(0.0f, 0.5f, 0.0f) * huiTranslation);
        // AddRenderItem(RenderLayer.Opaque, itemIndex++, "baseGeometry", "sphere", Matrix.Translation(2.0f, 0.5f, 0.0f) * huiTranslation);
        // AddRenderItem(RenderLayer.Opaque, itemIndex++, "baseGeometry", "cylinder", Matrix.Translation(1.0f, 1.5f, 0.0f) * huiTranslation);
        // AddRenderItem(RenderLayer.Opaque, itemIndex++, "baseGeometry", "sphere", Matrix.Translation(1.0f, 3.0f, 0.0f) * huiTranslation);
    }

    private void AddRenderItem(RenderLayer layer, ref int objConstantBufferIndex, MaterialsEnum matName, GeometryEnum geoName, MeshEnum submeshName, Matrix? world = null)
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
            World = submesh.World * world ?? Matrix.Identity,
            Mat = Materials[matName],
        };

        if (!SceneItemLayers.ContainsKey(layer))
            SceneItemLayers[layer] = [];

        SceneItemLayers[layer].Add(renderItem);
        SceneItems.Add(renderItem);
        objConstantBufferIndex++;
    }

    private void DrawRenderItems(GraphicsCommandList cmdList, List<RenderItem> ritems)
    {
        var objectCBSizeBytes = BufferUtil.CalcConstantBufferByteSize<ObjectConstants>();
        var materialCBSizeBytes = BufferUtil.CalcConstantBufferByteSize<ObjectConstants>();

        var objectCB = CurrentFrameResource.ObjectConstantBuffer.Resource;
        var materialCB = CurrentFrameResource.MaterialConstantBuffer.Resource;

        foreach (var item in ritems)
        {
            cmdList.SetVertexBuffer(0, item.Geo.VertexBufferView);
            cmdList.SetIndexBuffer(item.Geo.IndexBufferView);
            cmdList.PrimitiveTopology = item.PrimitiveType;

            var textureHandle = SRVDescriptorHeap.GPUDescriptorHandleForHeapStart + item.Mat.DiffuseSrvHeapIndex * CbvSrvUavDescriptorSize;

            var objectCBAddress = objectCB.GPUVirtualAddress + item.ObjCBIndex * objectCBSizeBytes;
            var materialCBAddress = materialCB.GPUVirtualAddress + item.Mat.MaterialCBIndex * materialCBSizeBytes;

            cmdList.SetGraphicsRootDescriptorTable(0, textureHandle);
            cmdList.SetGraphicsRootConstantBufferView(1, objectCBAddress);
            cmdList.SetGraphicsRootConstantBufferView(3, materialCBAddress);

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

        var transparentPsoDesc = opaquePsoDesc.Copy();

        var transparencyBlendDesc = new RenderTargetBlendDescription
        {
            IsBlendEnabled = true,
            LogicOpEnable = false,
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.Zero,
            AlphaBlendOperation = BlendOperation.Add,
            LogicOp = LogicOperation.Noop,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };
        transparentPsoDesc.BlendState.RenderTarget[0] = transparencyBlendDesc;

        PSOs[PSOEnum.Transparent] = RenderDevice.CreateGraphicsPipelineState(transparentPsoDesc);

        var alphaTestedPsoDesc = opaquePsoDesc.Copy();
        alphaTestedPsoDesc.PixelShader = PixelAlphatestdShaderByteCode;
        alphaTestedPsoDesc.RasterizerState.CullMode = CullMode.None;

        PSOs[PSOEnum.AlphaTested] = RenderDevice.CreateGraphicsPipelineState(alphaTestedPsoDesc);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        SRVDescriptorHeap?.Dispose();
        RenderRootSignature?.Dispose();
        CbvHeap?.Dispose();
        foreach (var frameResource in Frames)
            frameResource.Dispose();
        foreach (var geometry in Geometries.Values)
            geometry.Dispose();
        foreach (var texture in Textures.Values)
            texture.Dispose();
        foreach (var pso in PSOs.Values)
            pso.Dispose();

        GC.SuppressFinalize(this);

        base.Dispose();
    }
}
