using System.Runtime.InteropServices;
using System.Text;
using Refulgence.IO;
using Refulgence.Sm5;

namespace Refulgence.Dxbc;

public sealed class ShaderDxPart : DxPart
{
    public ushort      ProgramVersion;
    public ProgramType ProgramType;
    public uint[]      Tokens = [];

    public InstructionDecoder Instructions
        => new(Tokens);

    public static ShaderDxPart FromBytes(ReadOnlySpan<byte> data)
    {
        var part = new ShaderDxPart();
        var reader = new SpanBinaryReader(data);
        part.ProgramVersion = reader.Read<ushort>();
        part.ProgramType = reader.Read<ProgramType>();
        var tokenCount = reader.Read<uint>();
        part.Tokens = reader.Read<uint>((int)tokenCount - 2).ToArray();

        return part;
    }

    public static ShaderDxPart FromBytes(byte[] data)
        => FromBytes((ReadOnlySpan<byte>)data);

    public override void Dump(TextWriter writer)
    {
        writer.WriteLine($"{Tokens.Length + 2} (0x{Tokens.Length + 2:X4}) tokens");
        writer.WriteLine("    /* THIS DISASSEMBLER IS EXPERIMENTAL AND ITS OUTPUT MAY NOT BE COMPLETELY ACCURATE */");
        writer.WriteLine($"    {ProgramType.ToAbbreviation()}_{ProgramVersion >> 4}_{ProgramVersion & 0xF}");
        var indent = 0;
        foreach (var instruction in Instructions) {
            var opCodeInfo = instruction.OpCode.Type.GetInfo();
            if (opCodeInfo.Flags.HasFlag(OpCodeFlags.BlockEnd)) {
                --indent;
            }

            writer.WriteLine($"    {new string(' ', 2 * indent)}{instruction.ToString()}");
            if (opCodeInfo.Flags.HasFlag(OpCodeFlags.BlockStart)) {
                ++indent;
            }
        }
    }

    public override void WriteTo(Stream destination)
    {
        destination.Write(ProgramVersion);
        destination.Write(ProgramType);
        destination.Write((uint)(Tokens.Length + 2));
        destination.Write((ReadOnlySpan<uint>)Tokens);
    }
}
