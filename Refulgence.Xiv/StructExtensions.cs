using Refulgence.Dxbc.Signature;

namespace Refulgence.Xiv;

public static class StructExtensions
{
    public static VertexInput ToVertexInput(this SignatureElement element)
        => element.Name switch
        {
            "POSITION"    => element.SemanticIndex == 0 ? VertexInput.Position : 0,
            "BLENDWEIGHT" => element.SemanticIndex == 0 ? VertexInput.BlendWeight : 0,
            "NORMAL"      => element.SemanticIndex == 0 ? VertexInput.Normal : 0,
            "COLOR" => element.SemanticIndex switch
            {
                0 => VertexInput.Color0,
                1 => VertexInput.Color1,
                _ => 0,
            },
            "FOG"          => element.SemanticIndex == 0 ? VertexInput.Fog : 0,
            "PSIZE"        => element.SemanticIndex == 0 ? VertexInput.PSize : 0,
            "BLENDINDICES" => element.SemanticIndex == 0 ? VertexInput.BlendIndices : 0,
            "TEXCOORD" => element.SemanticIndex switch
            {
                0 => VertexInput.TexCoord0,
                1 => VertexInput.TexCoord1,
                2 => VertexInput.TexCoord2,
                3 => VertexInput.TexCoord3,
                4 => VertexInput.TexCoord4,
                5 => VertexInput.TexCoord5,
                _ => 0,
            },
            "TANGENT"  => element.SemanticIndex == 0 ? VertexInput.Tangent : 0,
            "BINORMAL" => element.SemanticIndex == 0 ? VertexInput.Binormal : 0,
            "DEPTH"    => element.SemanticIndex == 0 ? VertexInput.Depth : 0,
            _          => 0,
        };
}
