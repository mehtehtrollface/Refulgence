using System.Text;
using Refulgence.IO;

namespace Refulgence.Dxbc;

public sealed class StatsDxPart : DxPart
{
    public uint                       InstructionCount;
    public uint                       TempRegisterCount;
    public uint                       DefineCount;
    public uint                       DeclarationCount;
    public uint                       FloatInstructionCount;
    public uint                       IntInstructionCount;
    public uint                       UintInstructionCount;
    public uint                       StaticFlowControlCount;
    public uint                       DynamicFlowControlCount;
    public uint                       MacroInstructionCount;
    public uint                       TempArrayCount;
    public uint                       ArrayInstructionCount;
    public uint                       CutInstructionCount;
    public uint                       EmitInstructionCount;
    public uint                       TextureNormalInstructions;
    public uint                       TextureLoadInstructions;
    public uint                       TextureComparisonInstructions;
    public uint                       TextureBiasInstructions;
    public uint                       TextureGradientInstructions;
    public uint                       MovInstructionCount;
    public uint                       MovcInstructionCount;
    public uint                       ConversionInstructionCount;
    public uint                       Unknown1;
    public Primitive                  InputPrimitiveForGeometryShaders;
    public PrimitiveTopology          GeometryShaderPrimitiveTopology;
    public uint                       GeometryShaderMaxOutputVertexCount;
    public uint                       Unknown2;
    public uint                       Unknown3;
    public uint                       IsSampleFrequencyShader;
    public bool                       IsExtended;
    public uint                       Unknown4;
    public uint                       ControlPoints;
    public TessellatorOutputPrimitive HullShaderOutputPrimitive;
    public TessellatorPartitioning    HullShaderPartitioning;
    public TessellatorDomain          TessellatorDomain;
    public uint                       BarrierInstructions;
    public uint                       InterlockedInstructions;
    public uint                       TextureStoreInstructions;

    public static StatsDxPart FromBytes(ReadOnlySpan<byte> data)
    {
        var part = new StatsDxPart();
        var reader = new SpanBinaryReader(data);
        part.InstructionCount = reader.Read<uint>();
        part.TempRegisterCount = reader.Read<uint>();
        part.DefineCount = reader.Read<uint>();
        part.DeclarationCount = reader.Read<uint>();
        part.FloatInstructionCount = reader.Read<uint>();
        part.IntInstructionCount = reader.Read<uint>();
        part.UintInstructionCount = reader.Read<uint>();
        part.StaticFlowControlCount = reader.Read<uint>();
        part.DynamicFlowControlCount = reader.Read<uint>();
        part.MacroInstructionCount = reader.Read<uint>();
        part.TempArrayCount = reader.Read<uint>();
        part.ArrayInstructionCount = reader.Read<uint>();
        part.CutInstructionCount = reader.Read<uint>();
        part.EmitInstructionCount = reader.Read<uint>();
        part.TextureNormalInstructions = reader.Read<uint>();
        part.TextureLoadInstructions = reader.Read<uint>();
        part.TextureComparisonInstructions = reader.Read<uint>();
        part.TextureBiasInstructions = reader.Read<uint>();
        part.TextureGradientInstructions = reader.Read<uint>();
        part.MovInstructionCount = reader.Read<uint>();
        part.MovcInstructionCount = reader.Read<uint>();
        part.ConversionInstructionCount = reader.Read<uint>();
        part.Unknown1 = reader.Read<uint>();
        part.InputPrimitiveForGeometryShaders = reader.Read<Primitive>();
        part.GeometryShaderPrimitiveTopology = reader.Read<PrimitiveTopology>();
        part.GeometryShaderMaxOutputVertexCount = reader.Read<uint>();
        part.Unknown2 = reader.Read<uint>();
        part.Unknown3 = reader.Read<uint>();
        part.IsSampleFrequencyShader = reader.Read<uint>();
        if (reader.Remaining > 0) {
            part.IsExtended = true;
            part.Unknown4 = reader.Read<uint>();
            part.ControlPoints = reader.Read<uint>();
            part.HullShaderOutputPrimitive = reader.Read<TessellatorOutputPrimitive>();
            part.HullShaderPartitioning = reader.Read<TessellatorPartitioning>();
            part.TessellatorDomain = reader.Read<TessellatorDomain>();
            part.BarrierInstructions = reader.Read<uint>();
            part.InterlockedInstructions = reader.Read<uint>();
            part.TextureStoreInstructions = reader.Read<uint>();
        }

        return part;
    }

    public static StatsDxPart FromBytes(byte[] data)
        => FromBytes((ReadOnlySpan<byte>)data);

    public override void Dump(TextWriter writer)
    {
        writer.WriteLine(IsExtended ? "Extended stats" : "Stats");
        if (InstructionCount > 0) {
            writer.WriteLine($"    Instructions: {InstructionCount}");
        }

        if (TempRegisterCount > 0) {
            writer.WriteLine($"    Temp. registers: {TempRegisterCount}");
        }

        if (DefineCount > 0) {
            writer.WriteLine($"    Defines: {DefineCount}");
        }

        if (DeclarationCount > 0) {
            writer.WriteLine($"    Declarations: {DeclarationCount}");
        }

        if (FloatInstructionCount > 0) {
            writer.WriteLine($"    Float instructions: {FloatInstructionCount}");
        }

        if (IntInstructionCount > 0) {
            writer.WriteLine($"    Int instructions: {IntInstructionCount}");
        }

        if (UintInstructionCount > 0) {
            writer.WriteLine($"    UInt instructions: {UintInstructionCount}");
        }

        if (StaticFlowControlCount > 0) {
            writer.WriteLine($"    Static flow controls: {StaticFlowControlCount}");
        }

        if (DynamicFlowControlCount > 0) {
            writer.WriteLine($"    Dynamic flow controls: {DynamicFlowControlCount}");
        }

        if (MacroInstructionCount > 0) {
            writer.WriteLine($"    Macro instructions: {MacroInstructionCount}");
        }

        if (TempArrayCount > 0) {
            writer.WriteLine($"    Temp arrays: {TempArrayCount}");
        }

        if (ArrayInstructionCount > 0) {
            writer.WriteLine($"    Array instructions: {ArrayInstructionCount}");
        }

        if (CutInstructionCount > 0) {
            writer.WriteLine($"    Cut instructions: {CutInstructionCount}");
        }

        if (EmitInstructionCount > 0) {
            writer.WriteLine($"    Emit instructions: {EmitInstructionCount}");
        }

        if (TextureNormalInstructions > 0) {
            writer.WriteLine($"    Texture normal instructions: {TextureNormalInstructions}");
        }

        if (TextureLoadInstructions > 0) {
            writer.WriteLine($"    Texture load instructions: {TextureLoadInstructions}");
        }

        if (TextureComparisonInstructions > 0) {
            writer.WriteLine($"    Texture comparison instructions: {TextureComparisonInstructions}");
        }

        if (TextureBiasInstructions > 0) {
            writer.WriteLine($"    Texture bias instructions: {TextureBiasInstructions}");
        }

        if (TextureGradientInstructions > 0) {
            writer.WriteLine($"    Texture gradient instructions: {TextureGradientInstructions}");
        }

        if (MovInstructionCount > 0) {
            writer.WriteLine($"    Mov instructions: {MovInstructionCount}");
        }

        if (MovcInstructionCount > 0) {
            writer.WriteLine($"    MovC instructions: {MovcInstructionCount}");
        }

        if (ConversionInstructionCount > 0) {
            writer.WriteLine($"    Conversion instructions: {ConversionInstructionCount}");
        }

        if (Unknown1 > 0) {
            writer.WriteLine($"    Unknown 1: {Unknown1}");
        }

        if (InputPrimitiveForGeometryShaders != Primitive.Undefined) {
            writer.WriteLine($"    Input primitive for GS: {InputPrimitiveForGeometryShaders}");
        }

        if (GeometryShaderPrimitiveTopology != PrimitiveTopology.Undefined) {
            writer.WriteLine($"    GS primitive topology: {GeometryShaderPrimitiveTopology}");
        }

        if (GeometryShaderMaxOutputVertexCount > 0) {
            writer.WriteLine($"    GS max output vertices: {GeometryShaderMaxOutputVertexCount}");
        }

        if (Unknown2 > 0) {
            writer.WriteLine($"    Unknown 2: {Unknown2}");
        }

        if (Unknown3 > 0) {
            writer.WriteLine($"    Unknown 3: {Unknown3}");
        }

        if (IsSampleFrequencyShader > 0) {
            writer.WriteLine($"    Is sample frequency shader: {IsSampleFrequencyShader}");
        }

        if (!IsExtended) {
            return;
        }

        if (Unknown4 > 0) {
            writer.WriteLine($"    Unknown 4: {Unknown4}");
        }

        if (ControlPoints > 0) {
            writer.WriteLine($"    Control points: {ControlPoints}");
        }

        if (HullShaderOutputPrimitive != TessellatorOutputPrimitive.Undefined) {
            writer.WriteLine($"    HS output primitive: {HullShaderOutputPrimitive}");
        }

        if (HullShaderPartitioning != TessellatorPartitioning.Undefined) {
            writer.WriteLine($"    HS partitioning: {HullShaderPartitioning}");
        }

        if (TessellatorDomain != TessellatorDomain.Undefined) {
            writer.WriteLine($"    Tessellator domain: {TessellatorDomain}");
        }

        if (BarrierInstructions > 0) {
            writer.WriteLine($"    Barrier instructions: {BarrierInstructions}");
        }

        if (InterlockedInstructions > 0) {
            writer.WriteLine($"    Interlocked instructions: {InterlockedInstructions}");
        }

        if (TextureStoreInstructions > 0) {
            writer.WriteLine($"    Texture store instructions: {TextureStoreInstructions}");
        }
    }

    public override void WriteTo(Stream destination)
    {
        destination.Write(InstructionCount);
        destination.Write(TempRegisterCount);
        destination.Write(DefineCount);
        destination.Write(DeclarationCount);
        destination.Write(FloatInstructionCount);
        destination.Write(IntInstructionCount);
        destination.Write(UintInstructionCount);
        destination.Write(StaticFlowControlCount);
        destination.Write(DynamicFlowControlCount);
        destination.Write(MacroInstructionCount);
        destination.Write(TempArrayCount);
        destination.Write(ArrayInstructionCount);
        destination.Write(CutInstructionCount);
        destination.Write(EmitInstructionCount);
        destination.Write(TextureNormalInstructions);
        destination.Write(TextureLoadInstructions);
        destination.Write(TextureComparisonInstructions);
        destination.Write(TextureBiasInstructions);
        destination.Write(TextureGradientInstructions);
        destination.Write(MovInstructionCount);
        destination.Write(MovcInstructionCount);
        destination.Write(ConversionInstructionCount);
        destination.Write(Unknown1);
        destination.Write(InputPrimitiveForGeometryShaders);
        destination.Write(GeometryShaderPrimitiveTopology);
        destination.Write(GeometryShaderMaxOutputVertexCount);
        destination.Write(Unknown2);
        destination.Write(Unknown3);
        destination.Write(IsSampleFrequencyShader);
        if (!IsExtended) {
            return;
        }

        destination.Write(Unknown4);
        destination.Write(ControlPoints);
        destination.Write(HullShaderOutputPrimitive);
        destination.Write(HullShaderPartitioning);
        destination.Write(TessellatorDomain);
        destination.Write(BarrierInstructions);
        destination.Write(InterlockedInstructions);
        destination.Write(TextureStoreInstructions);
    }
}
