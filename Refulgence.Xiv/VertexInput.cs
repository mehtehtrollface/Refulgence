namespace Refulgence.Xiv;

[Flags]
public enum VertexInput : uint {
    Position     = 1 << 0,
    BlendWeight  = 1 << 1,
    Normal       = 1 << 2,
    Color0       = 1 << 3,
    Color1       = 1 << 4,
    Fog          = 1 << 5,
    PSize        = 1 << 6,
    BlendIndices = 1 << 7,
    TexCoord0    = 1 << 8,
    TexCoord1    = 1 << 9,
    TexCoord2    = 1 << 10,
    TexCoord3    = 1 << 11,
    TexCoord4    = 1 << 12,
    TexCoord5    = 1 << 13,
    Tangent      = 1 << 14,
    Binormal     = 1 << 15,
    Depth        = 1 << 16,
}
