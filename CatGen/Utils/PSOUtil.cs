using SharpDX.Direct3D12;

namespace CatGen.Utils;

public static class PSOUtil
{
    public static GraphicsPipelineStateDescription Copy(this GraphicsPipelineStateDescription desc)
    {
        var newDesc = new GraphicsPipelineStateDescription
        {
            BlendState = desc.BlendState,
            CachedPSO = desc.CachedPSO,
            DepthStencilFormat = desc.DepthStencilFormat,
            DepthStencilState = desc.DepthStencilState,
            SampleDescription = desc.SampleDescription,
            DomainShader = desc.DomainShader,
            Flags = desc.Flags,
            GeometryShader = desc.GeometryShader,
            HullShader = desc.HullShader,
            IBStripCutValue = desc.IBStripCutValue,
            InputLayout = desc.InputLayout,
            NodeMask = desc.NodeMask,
            PixelShader = desc.PixelShader,
            PrimitiveTopologyType = desc.PrimitiveTopologyType,
            RasterizerState = desc.RasterizerState,
            RenderTargetCount = desc.RenderTargetCount,
            SampleMask = desc.SampleMask,
            StreamOutput = desc.StreamOutput,
            VertexShader = desc.VertexShader,
            RootSignature = desc.RootSignature,
        };

        for (var i = 0; i < desc.RenderTargetFormats.Length; i++)
            newDesc.RenderTargetFormats[i] = desc.RenderTargetFormats[i];

        return newDesc;
    }
}
