using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Refulgence.Text;

namespace Refulgence.Sm5;

public ref struct Operand(
    OperandToken header,
    ReadOnlySpan<ExtendedOperandToken> extensions,
    ReadOnlySpan<uint> immediate,
    ReadOnlySpan<uint> index0,
    ReadOnlySpan<uint> index1,
    ReadOnlySpan<uint> index2)
{
    public readonly OperandToken                       Header     = header;
    public readonly ReadOnlySpan<ExtendedOperandToken> Extensions = extensions;
    public readonly ReadOnlySpan<uint>                 Immediate  = immediate;
    public readonly ReadOnlySpan<uint>                 Index0     = index0;
    public readonly ReadOnlySpan<uint>                 Index1     = index1;
    public readonly ReadOnlySpan<uint>                 Index2     = index2;

    public readonly bool TryGetExtension(ExtendedOperandType type, out ExtendedOperandToken token)
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

    public static Operand Decode(ReadOnlySpan<uint> tokens, int offset, out int nextOffset)
    {
        var header = new OperandToken(tokens[offset]);
        var extensions = 0;
        while (offset + 1 + extensions < tokens.Length && (tokens[offset + extensions] & 0x80000000u) != 0u) {
            ++extensions;
        }

        var extensionsSpan = MemoryMarshal.Cast<uint, ExtendedOperandToken>(tokens.Slice(offset + 1, extensions));
        offset += 1 + extensions;

        var immediateLength = header.Type switch
        {
            OperandType.Immediate32 => 1,
            OperandType.Immediate64 => 2,
            _                       => 0,
        } * header.NumComponents switch
        {
            OperandNumComponents.One  => 1,
            OperandNumComponents.Four => 4,
            _                         => 0,
        };
        var immediateSpan = tokens.Slice(offset, immediateLength);
        offset += immediateLength;

        var dimensions = header.IndexDimensions;
        var index0Span = dimensions > 0
            ? DecodeIndex(tokens, header.Index0Representation, offset, out offset)
            : ReadOnlySpan<uint>.Empty;
        var index1Span = dimensions > 1
            ? DecodeIndex(tokens, header.Index1Representation, offset, out offset)
            : ReadOnlySpan<uint>.Empty;
        var index2Span = dimensions > 2
            ? DecodeIndex(tokens, header.Index2Representation, offset, out offset)
            : ReadOnlySpan<uint>.Empty;
        nextOffset = offset;

        return new(header, extensionsSpan, immediateSpan, index0Span, index1Span, index2Span);
    }

    private static ReadOnlySpan<uint> DecodeIndex(ReadOnlySpan<uint> tokens, OperandIndexRepresentation representation,
        int offset, out int nextOffset)
    {
        var baseOffset = offset;
        offset += representation switch
        {
            OperandIndexRepresentation.Immediate32 or OperandIndexRepresentation.Immediate32PlusRelative => 1,
            OperandIndexRepresentation.Immediate64 or OperandIndexRepresentation.Immediate64PlusRelative => 2,
            _                                                                                            => 0,
        };

        if (representation is OperandIndexRepresentation.Relative or OperandIndexRepresentation.Immediate32PlusRelative
            or OperandIndexRepresentation.Immediate64PlusRelative) {
            Decode(tokens, offset, out offset);
        }

        nextOffset = offset;

        return tokens.Slice(baseOffset, offset - baseOffset);
    }

    private static string DecodeAndToString(ReadOnlySpan<uint> tokens, int offset)
        => Decode(tokens, offset, out _).ToString(false);

    public readonly override string ToString()
        => ToString(null);

    public readonly string ToString(bool? floatingPoint)
    {
        var modifier = TryGetExtension(ExtendedOperandType.Modifier, out var modifierExt)
            ? modifierExt.Modifier
            : OperandModifier.None;

        return modifier switch
        {
            OperandModifier.Neg    => $"-{ToStringImpl(floatingPoint)}",
            OperandModifier.Abs    => $"|{ToStringImpl(floatingPoint)}|",
            OperandModifier.AbsNeg => $"-|{ToStringImpl(floatingPoint)}|",
            _                      => ToStringImpl(floatingPoint),
        };
    }

    private readonly string ToStringImpl(bool? floatingPoint)
        => (Header.Type, Header.IndexDimensions) switch
        {
            (OperandType.Immediate32, _) => ToStringImm32(floatingPoint),
            (OperandType.Immediate64, _) => ToStringImm64(floatingPoint),
            (OperandType.Temp or OperandType.IndexableTemp or OperandType.Input or OperandType.Output or OperandType.Sampler
                or OperandType.Resource or OperandType.ConstantBuffer or OperandType.FunctionBody or OperandType.FunctionTable
                or OperandType.Interface or OperandType.UnorderedAccessView
                or OperandType.ThreadGroupSharedMemory, 0) => $"{Header.Type.ToPrefixString()}#{Header.ComponentsToString()}",
            (OperandType.ThisPointer, 0) => $"{Header.Type.ToPrefixString()}{Header.ComponentsToString()}",
            (OperandType.ConstantBuffer or OperandType.IndexableTemp or OperandType.Interface, > 1) =>
                $"{Header.Type.ToPrefixString()}{IndexToString(Header.Index0Representation, Index0, true)}[{IndexToString(Header.Index1Representation, Index1)}]{Header.ComponentsToString()}",
            (OperandType.Temp or OperandType.IndexableTemp or OperandType.Input or OperandType.Output or OperandType.Sampler
                or OperandType.Resource or OperandType.ConstantBuffer or OperandType.FunctionBody or OperandType.FunctionTable
                or OperandType.Interface or OperandType.UnorderedAccessView
                or OperandType.ThreadGroupSharedMemory, _) =>
                $"{Header.Type.ToPrefixString()}{IndexToString(Header.Index0Representation, Index0, true)}{Header.ComponentsToString()}",
            (OperandType.ThisPointer, _) =>
                $"{Header.Type.ToPrefixString()}[{IndexToString(Header.Index0Representation, Index0)}]{Header.ComponentsToString()}",
            (OperandType.Null, _) => "null",
            (_, _)                => $"<unresolved {Header.Type}`{Header.IndexDimensions}>",
        };

    private readonly string ToStringImm32(bool? floatingPoint)
    {
        var sb = new StringBuilder();
        sb.Append("l(");
        for (var i = 0; i < Immediate.Length; ++i) {
            if (i > 0) {
                sb.Append(", ");
            }

            sb.AppendImmediateToString(Immediate[i], floatingPoint);
        }

        sb.Append(')');

        return sb.ToString();
    }

    private readonly string ToStringImm64(bool? floatingPoint)
    {
        var sb = new StringBuilder();
        sb.Append("l(");
        for (var i = 0; i < Immediate.Length; i += 2) {
            if (i > 0) {
                sb.Append(", ");
            }

            sb.AppendImmediateToString(((ulong)Immediate[i] << 32) | Immediate[i + 1], floatingPoint);
        }

        sb.Append(')');

        return sb.ToString();
    }

    private static string IndexToString(OperandIndexRepresentation representation, ReadOnlySpan<uint> tokens,
        bool relativeBrackets = false)
        => (representation, relativeBrackets) switch
        {
            (OperandIndexRepresentation.Immediate32, _)                 => tokens[0].ToString(),
            (OperandIndexRepresentation.Immediate64, _)                 => (((ulong)tokens[0] << 32) | tokens[1]).ToString(),
            (OperandIndexRepresentation.Relative, false)                => DecodeAndToString(tokens, 0),
            (OperandIndexRepresentation.Relative, true)                 => $"[{DecodeAndToString(tokens, 0)}]",
            (OperandIndexRepresentation.Immediate32PlusRelative, false) => $"{DecodeAndToString(tokens,  1)} + {tokens[0]}",
            (OperandIndexRepresentation.Immediate32PlusRelative, true)  => $"[{DecodeAndToString(tokens, 1)} + {tokens[0]}]",
            (OperandIndexRepresentation.Immediate64PlusRelative, false) =>
                $"{DecodeAndToString(tokens, 2)} + {((ulong)tokens[0] << 32) | tokens[1]}",
            (OperandIndexRepresentation.Immediate64PlusRelative, true) =>
                $"[{DecodeAndToString(tokens, 2)} + {((ulong)tokens[0] << 32) | tokens[1]}]",
            _ => "???",
        };
}
