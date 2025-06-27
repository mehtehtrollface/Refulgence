using System.Runtime.InteropServices;
using System.Text;

namespace Refulgence.Sm5;

public ref struct Instruction(OpCodeToken opCode, ReadOnlySpan<ExtendedOpCodeToken> extensions, ReadOnlySpan<uint> operands)
{
    public readonly OpCodeToken                       OpCode     = opCode;
    public readonly ReadOnlySpan<ExtendedOpCodeToken> Extensions = extensions;
    public readonly ReadOnlySpan<uint>                Operands   = operands;

    public readonly bool TryGetExtension(ExtendedOpCodeType type, out ExtendedOpCodeToken token)
    {
        foreach (var extension in Extensions) {
            if (extension.Type == type) {
                token = extension;
                return true;
            }
        }

        token = default;
        return false;
    }

    public static Instruction Decode(ReadOnlySpan<uint> tokens, int offset, out int nextOffset)
    {
        var opCode = new OpCodeToken(tokens[offset]);
        if (opCode.Type is OpCodeType.CustomData) {
            var length = tokens[offset + 1];
            nextOffset = offset + Math.Max(2, checked((int)length));
            var operands = tokens[(offset + 2)..nextOffset];
            return new(opCode, [], operands);
        }

        var instruction = tokens.Slice(offset, opCode.Length);
        nextOffset = offset + Math.Max(1, (int)opCode.Length);
        var extensions = 0;
        while (1 + extensions < instruction.Length && (instruction[extensions] & 0x80000000u) != 0u) {
            ++extensions;
        }

        return new(opCode, MemoryMarshal.Cast<uint, ExtendedOpCodeToken>(instruction[1..(1 + extensions)]), instruction[(1 + extensions)..]);
    }

    public readonly override string ToString()
    {
        if (OpCode.Type is OpCodeType.CustomData) {
            return ToStringCustomData();
        }

        var opCodeInfo = OpCodeInfo.ForOpCode(OpCode.Type);
        if (opCodeInfo.Flags.HasFlag(OpCodeFlags.CustomOperands)) {
            return ToStringCustomOperands();
        }

        var sb = new StringBuilder();
        sb.Append(OpCodeInfo.ForOpCode(OpCode.Type).Mnemonic);
        if (OpCode.Saturate) {
            sb.Append("_sat");
        }

        if (opCodeInfo.Flags.HasFlag(OpCodeFlags.HasTest)) {
            sb.Append(OpCode.Test.ToSuffix());
        }

        if (OpCode.Type is OpCodeType.ResInfo or OpCodeType.SampleInfo) {
            switch (OpCode.ResInfoReturnType) {
                case ResInfoReturnType.UInt:
                    sb.Append("_uint");
                    break;
                case ResInfoReturnType.RcpFloat:
                    sb.Append("_rcpFloat");
                    break;
            }
        } else if (OpCode.Type is OpCodeType.Sync) {
            var flags = OpCode.SyncFlags;
            if (flags.HasFlag(SyncFlags.UnorderedAccessViewMemoryGlobal)) {
                sb.Append("_uglobal");
            }

            if (flags.HasFlag(SyncFlags.UnorderedAccessViewMemoryGroup)) {
                sb.Append("_ugroup");
            }

            if (flags.HasFlag(SyncFlags.ThreadGroupSharedMemory)) {
                sb.Append("_g");
            }

            if (flags.HasFlag(SyncFlags.ThreadsInGroup)) {
                sb.Append("_t");
            }
        }

        if (TryGetExtension(ExtendedOpCodeType.SampleControls, out var sampleControls)) {
            sb.Append($"_aoffimmi({sampleControls.OffsetU},{sampleControls.OffsetV},{sampleControls.OffsetW})");
        }

        foreach (var ext in Extensions) {
            if ((byte)ext.Type < 4) {
                sb.Append("_indexable");
                break;
            }
        }

        if (TryGetExtension(ExtendedOpCodeType.ResourceDim, out var resourceDim)) {
            sb.Append($"({resourceDim.ResourceDimension.ToLowerString()})");
        }

        if (TryGetExtension(ExtendedOpCodeType.ResourceReturnType, out var resourceReturnType)) {
            sb.Append(
                $"({resourceReturnType.ResourceReturnTypeX.ToLowerString()},{resourceReturnType.ResourceReturnTypeY.ToLowerString()},{resourceReturnType.ResourceReturnTypeZ.ToLowerString()},{resourceReturnType.ResourceReturnTypeW.ToLowerString()})"
            );
        }

        var first = true;
        foreach (var operand in new OperandDecoder(Operands)) {
            if (first) {
                sb.Append(' ');
                first = false;
            } else {
                sb.Append(", ");
            }

            sb.Append(operand.ToString());
        }

        return sb.ToString();
    }

    private readonly string ToStringCustomOperands()
    {
        var sb = new StringBuilder();
        int offset;

        sb.Append(OpCodeInfo.ForOpCode(OpCode.Type).Mnemonic);
        switch (OpCode.Type) {
            case OpCodeType.DclGlobalFlags:
                var first = true;
                var flags = OpCode.GlobalFlags;
                while (0 != flags) {
                    if (first) {
                        first = false;
                    } else {
                        sb.Append(" |");
                    }

                    sb.Append($" {(flags & unchecked((GlobalFlags)(-(int)flags))).ToCamelString()}");

                    flags &= unchecked((GlobalFlags)((int)flags - 1));
                }

                break;
            case OpCodeType.DclResource:
                sb.Append($"_{OpCode.ResourceDimension.ToLowerString()}");
                if (OpCode.ResourceDimension is ResourceDimension.Texture2DMS or ResourceDimension.Texture2DMSArray) {
                    sb.Append($"({OpCode.NumSamples})");
                }

                var operand = Operand.Decode(Operands, 0, out offset);
                var returnType = new ResourceReturnTypeToken(Operands[offset]);

                sb.Append(
                    $" ({returnType.ResourceReturnTypeX.ToLowerString()},{returnType.ResourceReturnTypeY.ToLowerString()},{returnType.ResourceReturnTypeZ.ToLowerString()},{returnType.ResourceReturnTypeW.ToLowerString()}) {operand.ToString()}"
                );
                break;
            case OpCodeType.DclSampler:
                operand = Operand.Decode(Operands, 0, out offset);
                sb.Append($" {operand.ToString()}, mode_{OpCode.SamplerMode.ToLowerString()}");
                break;
            case OpCodeType.DclInputSiv:
            case OpCodeType.DclInputSgv:
            case OpCodeType.DclOutputSiv:
            case OpCodeType.DclOutputSgv:
                operand = Operand.Decode(Operands, 0, out offset);
                sb.Append($" {operand.ToString()}, {((Name)Operands[offset]).ToLowerString()}");
                break;
            case OpCodeType.DclInputPs:
                operand = Operand.Decode(Operands, 0, out offset);
                if (OpCode.InterpolationMode != InterpolationMode.Undefined) {
                    sb.Append($" {OpCode.InterpolationMode.ToLowerWordsString(' ')}");
                }

                sb.Append($" {operand.ToString()}");
                break;
            case OpCodeType.DclInputPsSiv:
            case OpCodeType.DclInputPsSgv:
                operand = Operand.Decode(Operands, 0, out offset);
                if (OpCode.InterpolationMode != InterpolationMode.Undefined) {
                    sb.Append($" {OpCode.InterpolationMode.ToLowerWordsString(' ')}");
                }

                sb.Append($" {operand.ToString()}, {((Name)Operands[offset]).ToLowerString()}");
                break;
            case OpCodeType.DclIndexRange:
            case OpCodeType.DclThreadGroupSharedMemoryRaw:
            case OpCodeType.DclResourceStructured:
                operand = Operand.Decode(Operands, 0, out offset);
                sb.Append($" {operand.ToString()}, {Operands[offset]}");
                break;
            case OpCodeType.DclTemps:
            case OpCodeType.DclMaxOutputVertexCount:
            case OpCodeType.DclGsInstanceCount:
            case OpCodeType.DclHsForkPhaseInstanceCount:
            case OpCodeType.DclHsJoinPhaseInstanceCount:
                sb.Append($" {Operands[0]}");
                break;
            case OpCodeType.DclIndexableTemp:
                sb.Append($" x{Operands[0]}[{Operands[1]}].{((ComponentMask)((1 << (int)Operands[2]) - 1)).ToCompactString()}");
                break;
            case OpCodeType.DclConstantBuffer:
                operand = Operand.Decode(Operands, 0, out offset);
                sb.Append($" {operand.ToString()}, {OpCode.ConstantBufferAccessPattern.ToCamelString()}");
                break;
            case OpCodeType.DclGsInputPrimitive:
                sb.Append($" {OpCode.Primitive.ToLowerString()}");
                break;
            case OpCodeType.DclGsOutputPrimitiveTopology:
                sb.Append($" {OpCode.PrimitiveTopology.ToLowerString()}");
                break;
            case OpCodeType.DclInputControlPointCount:
            case OpCodeType.DclOutputControlPointCount:
                sb.Append($" {OpCode.ControlPointCount}");
                break;
            case OpCodeType.DclTessDomain:
                sb.Append($" {OpCode.TessellatorDomain.ToLowerString()}");
                break;
            case OpCodeType.DclTessPartitioning:
                sb.Append($" {OpCode.TessellatorPartitioning.ToLowerString()}");
                break;
            case OpCodeType.DclTessOutputPrimitive:
                sb.Append($" {OpCode.TessellatorOutputPrimitive.ToLowerString()}");
                break;
            case OpCodeType.DclHsMaxTessfactor:
                sb.Append($" {BitConverter.UInt32BitsToSingle(Operands[0])}");
                break;
            case OpCodeType.DclFunctionBody:
                sb.Append($" fb{Operands[0]}");
                break;
            case OpCodeType.DclFunctionTable:
                sb.Append($" ft{Operands[0]} = {{");
                first = true;
                for (var i = 0; i < Operands[1]; ++i) {
                    if (first) {
                        first = false;
                    } else {
                        sb.Append(", ");
                    }

                    sb.Append($"fb{Operands[i + 2]}");
                }

                sb.Append('}');
                break;
            case OpCodeType.DclInterface:
                sb.Append($" fp{Operands[0]}[{Operands[2] >> 16}][{Operands[1]}] = {{");
                first = true;
                var tableLength = Operands[2] & 0xFFFFu;
                for (var i = 0; i < tableLength; ++i) {
                    if (first) {
                        first = false;
                    } else {
                        sb.Append(", ");
                    }

                    sb.Append($"ft{Operands[i + 3]}");
                }

                sb.Append('}');
                break;
            case OpCodeType.DclThreadGroup:
                sb.Append($" {Operands[0]}, {Operands[1]}, {Operands[2]}");
                break;
            case OpCodeType.DclUnorderedAccessViewTyped:
                if (OpCode.GloballyCoherentAccess) {
                    sb.Append("_glc");
                }

                operand = Operand.Decode(Operands, 0, out offset);
                returnType = new(Operands[offset]);

                sb.Append(
                    $" {operand.ToString()}, {OpCode.ResourceDimension.ToLowerString()}, ({returnType.ResourceReturnTypeX.ToLowerString()},{returnType.ResourceReturnTypeY.ToLowerString()},{returnType.ResourceReturnTypeZ.ToLowerString()},{returnType.ResourceReturnTypeW.ToLowerString()})"
                );
                break;
            case OpCodeType.DclUnorderedAccessViewRaw:
                if (OpCode.GloballyCoherentAccess) {
                    sb.Append("_glc");
                }

                operand = Operand.Decode(Operands, 0, out offset);
                sb.Append($" {operand.ToString()}");
                break;
            case OpCodeType.DclUnorderedAccessViewStructured:
                if (OpCode.GloballyCoherentAccess) {
                    sb.Append("_glc");
                }

                if (OpCode.UavHasOrderPreservingCounter) {
                    sb.Append("_opc");
                }

                operand = Operand.Decode(Operands, 0, out offset);
                sb.Append($" {operand.ToString()}, {Operands[offset]}");
                break;
            case OpCodeType.DclThreadGroupSharedMemoryStructured:
                operand = Operand.Decode(Operands, 0, out offset);
                sb.Append($" {operand.ToString()}, {Operands[offset]}, {Operands[offset + 1]}");
                break;
            case OpCodeType.InterfaceCall:
                operand = Operand.Decode(Operands, 1, out offset);
                sb.Append($" {operand.ToString()}[{Operands[0]}]");
                break;
            default:
                sb.Append(" <unknown operands>");
                foreach (var token in Operands) {
                    sb.Append($" {token:X8}");
                }

                break;
        }

        return sb.ToString();
    }

    private readonly string ToStringCustomData()
        => "custom_data <unknown operands>";
}
