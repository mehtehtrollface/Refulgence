using System.ComponentModel;
using System.Runtime.CompilerServices;
using Refulgence.Text;

namespace Refulgence.Sm5;

public static class EnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OpCodeInfo GetInfo(this OpCodeType opCode)
        => OpCodeInfo.ForOpCode(opCode);

    public static string ToSuffix(this Test test)
        => test switch
        {
            Test.Zero    => "_z",
            Test.NonZero => "_nz",
            _            => throw new InvalidEnumArgumentException($"Invalid test {test}"),
        };

    public static string ToSpacedString(this ComponentMask components)
        => $"{(components.HasFlag(ComponentMask.X) ? 'x' : ' ')}{(components.HasFlag(ComponentMask.Y) ? 'y' : ' ')}{(components.HasFlag(ComponentMask.Z) ? 'z' : ' ')}{(components.HasFlag(ComponentMask.W) ? 'w' : ' ')}";

    public static string ToCompactString(this ComponentMask components)
        => $"{(components.HasFlag(ComponentMask.X) ? "x" : string.Empty)}{(components.HasFlag(ComponentMask.Y) ? "y" : string.Empty)}{(components.HasFlag(ComponentMask.Z) ? "z" : string.Empty)}{(components.HasFlag(ComponentMask.W) ? "w" : string.Empty)}";

    public static string ToPrefixString(this OperandType type)
        => type switch
        {
            OperandType.Temp                                   => "r",
            OperandType.Input                                  => "v",
            OperandType.Output                                 => "o",
            OperandType.IndexableTemp                          => "x",
            OperandType.Immediate32 or OperandType.Immediate64 => "l",
            OperandType.Sampler                                => "s",
            OperandType.Resource                               => "t",
            OperandType.ConstantBuffer                         => "cb",
            OperandType.FunctionBody                           => "fb",
            OperandType.FunctionTable                          => "ft",
            OperandType.Interface                              => "fp",
            OperandType.ThisPointer                            => "this",
            OperandType.UnorderedAccessView                    => "u",
            OperandType.ThreadGroupSharedMemory                => "g",
            _                                                  => "???",
        };

    internal static string ToLowerString<T>(this T value) where T : Enum
        => value.ToString().ToLowerInvariant();

    internal static string ToCamelString<T>(this T value) where T : Enum
        => value.ToString().PascalToCamelInvariant();

    internal static string ToLowerWordsString<T>(this T value, char wordSeparator) where T : Enum
        => value.ToString().PascalToLowerWordsInvariant(wordSeparator);
}
