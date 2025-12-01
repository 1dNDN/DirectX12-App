using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows.Forms;

using RealGen.Unifiers;
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
    protected readonly Dictionary<string, MeshGeometry> Geometries = new();

    /// <summary>
    /// Материалы для геометрий сцены
    /// </summary>
    private Dictionary<string, Material> Materials { get; set; } = new();

    /// <summary>
    /// Текстуры для геометрий сцены
    /// </summary>
    private readonly Dictionary<string, Texture> Textures = new();

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
    protected PassConstants MainPassConstantBuffer = PassConstants.Default;

    //TODO:
    protected int _passCbvOffset;

    /// <summary>
    /// Рисовать ли полигоны в виде сетки или как настоящие. True - в виде сетки, False - как настоящие
    /// </summary>
    protected bool IsWireframe = false;

    private Vector3 EyePosition;

    /// <summary>
    /// Матрица проекции
    /// </summary>
    protected Matrix Proj { get; set; } = Matrix.Identity;

    /// <summary>
    /// Матрица камеры
    /// </summary>
    protected Matrix View { get; set; } = Matrix.Identity;

    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        RenderCommandList.Reset(RenderDirectCmdListAlloc, null);

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
        CommandAllocator cmdListAlloc = CurrentFrameResource.CmdListAlloc;


        // Reuse the memory associated with command recording.
        // We can only reset when the associated command lists have finished execution on the GPU.
        cmdListAlloc.Reset();

        // A command list can be reset after it has been added to the command queue via ExecuteCommandList.
        // Reusing the command list reuses memory.
        RenderCommandList.Reset(cmdListAlloc, IsWireframe ? PSOs["opaque_wireframe"] : PSOs["opaque"]);

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

        RenderCommandList.SetDescriptorHeaps(1, DescriptorHeaps);

        RenderCommandList.SetGraphicsRootSignature(RenderRootSignature);

        var passCB = CurrentFrameResource.PassConstantBuffer.Resource;
        RenderCommandList.SetGraphicsRootConstantBufferView(2, passCB.GPUVirtualAddress);

        DrawRenderItems(RenderCommandList, SceneItemLayers[RenderLayer.Opaque]);

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

    /// <summary>
    /// Азимутальный угол
    /// </summary>
    private float _theta = 1.5f * MathUtil.Pi;

    /// <summary>
    /// Зенитный угол
    /// </summary>
    private float _phi = MathUtil.PiOverFour;

    /// <summary>
    ///  Расстояние от камеры до начала координат
    /// </summary>
    private float _radius = 5.0f;

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
            // Один пиксель - четверть градуса.
            var dx = MathUtil.DegreesToRadians(0.25f * (location.X - _lastMousePos.X));
            var dy = MathUtil.DegreesToRadians(0.25f * (location.Y - _lastMousePos.Y));

            _theta += dx;
            _phi += dy;

            // Ограничиваем зенитный угол
            // _phi = MathUtil.Clamp(_phi, 0.1f, MathUtil.Pi - 0.1f);
        }
        else if ((button & MouseButtons.Right) != 0)
        {
            // Один пиксель - четверть градуса.
            var dx = 0.005f * (location.X - _lastMousePos.X);
            var dy = 0.005f * (location.Y - _lastMousePos.Y);

            _radius += dx - dy;

            // Ограничиваем радиус
            // _radius = MathUtil.Clamp(_radius, 3.0f, 15.0f);
        }

        _lastMousePos = location;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(Keys keyCode)
    {
        if (keyCode == Keys.D1)
            IsWireframe = true;

        var dx = 0.1f;

        if (keyCode == Keys.Left)
            _theta += dx;

        if (keyCode == Keys.Right)
            _theta -= dx;

        var dy = 0.1f;

        if (keyCode == Keys.Up)
            _phi += dy;

        if (keyCode == Keys.Down)
            _phi -= dy;

        // _phi = MathUtil.Clamp(_phi, 0.1f, MathUtil.Pi - 0.1f);

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
        Proj = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, AspectRatio, 1.0f, 1000.0f);
    }

    /// <summary>
    /// Создаёт кучу с дескрипторами
    /// </summary>
    private void BuildDescriptorHeaps()
    {
        var srvHeapDesc = new DescriptorHeapDescription
        {
            DescriptorCount = 1,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            Flags = DescriptorHeapFlags.ShaderVisible
        };
        SRVDescriptorHeap = RenderDevice.CreateDescriptorHeap(srvHeapDesc);
        DescriptorHeaps = [SRVDescriptorHeap];

        var descriptorHandle = SRVDescriptorHeap.CPUDescriptorHandleForHeapStart;
        var texture = Textures["duck"].Resource;

        var srvDescription = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = TextureUtil.DefaultShader4ComponentMapping,
            Format = texture.Description.Format,
            Dimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new ShaderResourceViewDescription.Texture2DResource
            {
                MostDetailedMip = 0,
                MipLevels = texture.Description.MipLevels,
                ResourceMinLODClamp = 0.0f,
            },
        };

        RenderDevice.CreateShaderResourceView(texture, srvDescription, descriptorHandle);
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
        var (texture, sampler) = GLTFReader.ImportTexture("./Models/Duck/glTF-Embedded/Duck.gltf");
        var boxTexture = new Texture()
        {
            Name = "duck",
        };

        boxTexture.Resource = TextureUtil.CreateTextureFromPNG(RenderDevice, texture);
        Textures[boxTexture.Name] = boxTexture;

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
        VertexShaderByteCode = ShaderUtil.CompileShader("Shaders/vertex.hlsl", "VS", "vs_5_0");
        PixelShaderByteCode = ShaderUtil.CompileShader("Shaders/pixel.hlsl", "PS", "ps_5_0");

        ShaderInputLayout = new InputLayoutDescription(
        [
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
            new InputElement("TANGENT", 0, Format.R32G32B32_Float, 32, 0)
        ]);
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

        var geo = MeshGeometry.New(RenderDevice, RenderCommandList, vertices, indices.ToArray(), "baseGeometry");

        geo.DrawArgs["box"] = box;
        geo.DrawArgs["grid"] = grid;
        geo.DrawArgs["sphere"] = sphere;
        geo.DrawArgs["cylinder"] = cylinder;
        geo.DrawArgs["jenjina"] = jenjina;
        geo.DrawArgs["superbox"] = superbox;
        geo.DrawArgs["duck"] = duck;
        geo.DrawArgs["big_dragon"] = bigDragon;

        Geometries[geo.Name] = geo;
    }

    private void BuildFrameResources()
    {
        for (var i = 0; i < NumFrameResources; i++)
        {
            Frames.Add(new Frame(RenderDevice, 1, SceneItems.Count, Materials.Count));
            FenceEvents.Add(new AutoResetEvent(false));
        }
    }

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

        AddMaterial(new Material
        {
            Name = "duck",
            MaterialCBIndex = 0,
            DiffuseSrvHeapIndex = 0,
            DiffuseAlbedo = Color.White.ToVector4(),
            FresnelR0 = new Vector3(0.05f),
            Roughness = 0.2f
        });

    }

    private void AddMaterial(Material mat) => Materials[mat.Name] = mat;


    [SuppressMessage("ReSharper", "RedundantAssignment")]
    private void BuildRenderItems()
    {
        var itemIndex = 0;

        AddRenderItem(RenderLayer.Opaque, itemIndex++, "duck", "baseGeometry", "duck",
            Matrix.Translation(0.0f, 0.0f, 0.0f) *
            Matrix.Scaling(1.0F) *
            Matrix.RotationYawPitchRoll(0.0F, -float.Pi/2, float.Pi));


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

    private void AddRenderItem(RenderLayer layer, int objConstantBufferIndex, string matName, string geoName, string submeshName, Matrix? world = null)
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

        SceneItemLayers[layer].Add(renderItem);
        SceneItems.Add(renderItem);
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

        PSOs["opaque"] = RenderDevice.CreateGraphicsPipelineState(opaquePsoDesc);

        var opaqueWireframePsoDesc = opaquePsoDesc;
        opaqueWireframePsoDesc.RasterizerState.FillMode = FillMode.Wireframe;

        PSOs["opaque_wireframe"] = RenderDevice.CreateGraphicsPipelineState(opaqueWireframePsoDesc);
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
