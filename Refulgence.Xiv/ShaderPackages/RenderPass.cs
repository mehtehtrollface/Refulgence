namespace Refulgence.Xiv.ShaderPackages;

public sealed class RenderPass(Name name, int vertexShaderIndex, int pixelShaderIndex)
{
    public Name Name              = name;
    public int  PixelShaderIndex  = pixelShaderIndex;
    public int  VertexShaderIndex = vertexShaderIndex;
}
