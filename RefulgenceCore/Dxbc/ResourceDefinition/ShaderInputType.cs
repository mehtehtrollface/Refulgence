namespace Refulgence.Dxbc.ResourceDefinition;

public enum ShaderInputType : uint
{
    CBuffer = 0,
    TBuffer,
    Texture,
    Sampler,
    UavRWTyped,
    Structured,
    UavRWStructured,
    ByteAddress,
    UavRWByteAddress,
    UavAppendStructured,
    UavConsumeStructured,
    UavRWStructuredWithCounter,
    RtAccelerationStructure,
    UavFeedbackTexture,
}
