using Refulgence.Text;

namespace Refulgence.Xiv.IO;

internal static partial class ShaderReaderWriter
{
    private sealed class CodeWriter(Shader shader, Stream destination) : Writer(destination)
    {
        protected override InlineByteString<uint> Magic
            => new("ShCd"u8);

        protected override uint Version
            => ((uint)shader.ProgramType << 24) | 0x0601u;

        protected override GraphicsPlatform GraphicsPlatform
            => shader.GraphicsPlatform;

        protected override void DoWrite()
            => WriteShader(shader, 0u);
    }
}
