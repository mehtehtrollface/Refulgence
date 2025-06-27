using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Refulgence.Cli.IO;
using Refulgence.Collections;
using Refulgence.Text;
using Refulgence.Xiv;
using Refulgence.Xiv.ShaderPackages;

namespace Refulgence.Cli.Programs;

public static class ShaderPackageUpdate
{
    public static int Run(string inputFileName, string outputFileName, ReadOnlySpan<string> instructions)
    {
        var shpk = ShaderPackage.FromShaderPackageBytes(File.ReadAllBytes(inputFileName));

        var offset = 0;
        while (offset < instructions.Length) {
            RunInstruction(shpk, instructions, ref offset);
        }

        File.WriteAllBytes(outputFileName, shpk.ToShaderPackageBytes());

        return 0;
    }

    private static void RunInstruction(ShaderPackage shpk, ReadOnlySpan<string> instructions, ref int offset)
    {
        var updateResources = false;
        switch (instructions[offset].ToLowerInvariant()) {
            case "mp+":
                AddMaterialParameter(shpk, instructions[offset + 1]);
                updateResources = true;
                offset += 2;
                break;
            case "mk+":
                AddMaterialKey(shpk, instructions[offset + 1]);
                offset += 2;
                break;
            case "ct=":
                ConfigureResource(shpk.ConstantBuffers, instructions[offset + 1]);
                updateResources = true;
                offset += 2;
                break;
            case "st=":
                ConfigureResource(shpk.Samplers, instructions[offset + 1]);
                updateResources = true;
                offset += 2;
                break;
            case "tt=":
                ConfigureResource(shpk.Textures, instructions[offset + 1]);
                updateResources = true;
                offset += 2;
                break;
            case "ut=":
                ConfigureResource(shpk.Uavs, instructions[offset + 1]);
                updateResources = true;
                offset += 2;
                break;
            default:
                ReplaceOrAddShader(shpk, instructions[offset], instructions[offset + 1]);
                updateResources = true;
                offset += 2;
                break;
        }

        if (updateResources) {
            shpk.UpdateResources();
        }
    }

    private static void AddMaterialParameter(ShaderPackage shpk, string instruction)
    {
        var tokens = instruction.Split(':');
        MaterialParameter param;
        if (tokens[1].Length > 0 && char.ToLowerInvariant(tokens[1][^1]) is >= 'w' and <= 'z') {
            var component = char.ToLowerInvariant(tokens[1][^1]) switch
            {
                'w' => 0xC,
                'x' => 0x0,
                'y' => 0x4,
                'z' => 0x8,
                _ => throw new(
                    $"Component suffix {tokens[1][^1]} accepted by prior condition, but unrecognized. This should never happen."
                ),
            };
            param = new(
                tokens[0], (ushort)((ushort.Parse(tokens[1].AsSpan(..^1)) << 4) | component), (ushort)(ushort.Parse(tokens[2]) << 2)
            );
        } else {
            param = new(tokens[0], ushort.Parse(tokens[1]), ushort.Parse(tokens[2]));
        }

        shpk.MaterialParameters.Add(param);
        Console.Error.WriteLine($"Added material parameter {param.Name}");
        if (shpk.MaterialParametersDefaults is null) {
            return;
        }

        if (shpk.MaterialParametersDefaults.Length < param.ByteOffset + param.ByteSize) {
            Array.Resize(ref shpk.MaterialParametersDefaults, param.ByteOffset + param.ByteSize);
        }

        if (tokens.Length <= 3) {
            return;
        }

        var rawDefaults = shpk.MaterialParametersDefaults.AsSpan(param.ByteOffset, param.ByteSize);
        var type = tokens.Length > 4 ? tokens[3].ToLowerInvariant() : "f";
        var values = tokens[tokens.Length > 4 ? 4 : 3].Split(',');
        switch (type) {
            case "i":
            case "s":
                var iDefaults = MemoryMarshal.Cast<byte, int>(rawDefaults);
                for (var i = 0; i < values.Length; ++i) {
                    iDefaults[i] = values[i].StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? int.Parse(values[i].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                        : values[i].StartsWith("-0x", StringComparison.OrdinalIgnoreCase)
                            ? int.Parse(
                                string.Concat("-", values[i].AsSpan(3)), NumberStyles.HexNumber, CultureInfo.InvariantCulture
                            )
                            : int.Parse(values[i], CultureInfo.InvariantCulture);
                }

                break;
            case "u":
                var uDefaults = MemoryMarshal.Cast<byte, uint>(rawDefaults);
                for (var i = 0; i < values.Length; ++i) {
                    uDefaults[i] = values[i].StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? uint.Parse(values[i].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                        : uint.Parse(values[i],           CultureInfo.InvariantCulture);
                }

                break;
            default:
                var fDefaults = MemoryMarshal.Cast<byte, float>(rawDefaults);
                for (var i = 0; i < values.Length; ++i) {
                    fDefaults[i] = float.Parse(values[i], CultureInfo.InvariantCulture);
                }

                break;
        }

        Console.Error.WriteLine($"Set default for material parameter {param.Name}");
    }

    private static void AddMaterialKey(ShaderPackage shpk, string instruction)
    {
        var tokens = instruction.Split(':');
        var key = new Name(tokens[0]);
        var defaultValue = key + tokens[1];
        var alternateValues = new Dictionary<Name, Dictionary<ProgramType, Dictionary<int, int>>>();
        foreach (var altToken in tokens.AsSpan(2)) {
            var subTokens = altToken.Split(',');
            var value = key + subTokens[0];
            var replacements = new Dictionary<ProgramType, Dictionary<int, int>>();
            alternateValues.Add(value, replacements);
            foreach (var subToken in subTokens.AsSpan(1)) {
                var (replacement, original) = subToken.SplitOnce('/');
                var (programType, replacementIndex) = ShaderPackageExtract.ParseShaderId(replacement);
                var originalIndex = uint.Parse(original ?? string.Empty);
                if (!replacements.TryGetValue(programType, out var replacementsOfType)) {
                    replacementsOfType = new();
                    replacements.Add(programType, replacementsOfType);
                }

                replacementsOfType.Add((int)originalIndex, (int)replacementIndex);
            }
        }

        var multiplier = ShaderPackage.SelectorMultiplier * ShaderPackage.SelectorMultiplier;
        for (var i = 0; i < shpk.MaterialKeys.Count; ++i) {
            multiplier *= ShaderPackage.SelectorMultiplier;
        }

        var shaderKey = new ShaderKey(key, defaultValue);
        alternateValues.Keys.AddTo(shaderKey.Values);
        shpk.MaterialKeys.Add(shaderKey);

        var nodeReplacements = new Dictionary<Name, Dictionary<int, int>>();
        var nodeCount = shpk.RenderNodes.Count;
        for (var i = 0; i < nodeCount; ++i) {
            var node = shpk.RenderNodes[i];
            foreach (var (value, replacements) in alternateValues) {
                var anyReplacement = false;
                var newNode = new RenderNode(unchecked(node.PrimarySelector + value.Crc32 * multiplier));
                node.SystemValues.AddTo(newNode.SystemValues);
                node.SceneValues.AddTo(newNode.SceneValues);
                node.MaterialValues.AddTo(newNode.MaterialValues);
                newNode.SubViewValue0 = node.SubViewValue0;
                newNode.SubViewValue1 = node.SubViewValue1;
                node.PassIndices.CopyTo(newNode.PassIndices, 0);
                foreach (var pass in node.Passes) {
                    if (!replacements.TryGetValue(ProgramType.VertexShader, out var vsReplacements)
                     || !vsReplacements.TryGetValue(pass.VertexShaderIndex, out var vsIndex)) {
                        vsIndex = pass.VertexShaderIndex;
                    }

                    if (!replacements.TryGetValue(ProgramType.VertexShader, out var psReplacements)
                     || !psReplacements.TryGetValue(pass.PixelShaderIndex, out var psIndex)) {
                        psIndex = pass.PixelShaderIndex;
                    }

                    newNode.Passes.Add(new(pass.Name, vsIndex, psIndex));
                    anyReplacement |= vsIndex != pass.VertexShaderIndex || psIndex != pass.PixelShaderIndex;
                }

                if (anyReplacement) {
                    if (!nodeReplacements.TryGetValue(value, out var nodeReplacementsOfValue)) {
                        nodeReplacementsOfValue = new();
                        nodeReplacements.Add(value, nodeReplacementsOfValue);
                    }

                    nodeReplacementsOfValue.Add(i, shpk.RenderNodes.Count);
                    shpk.RenderNodes.Add(newNode);
                }
            }

            node.MaterialValues.Add(key, defaultValue);
            node.PrimarySelector = unchecked(node.PrimarySelector + defaultValue.Crc32 * multiplier);
        }

        var oldSelectors = shpk.RenderNodeSelectors.ToDictionary();
        shpk.RenderNodeSelectors.Clear();
        foreach (var (selector, index) in oldSelectors) {
            shpk.RenderNodeSelectors.Add(unchecked(selector + shaderKey.DefaultValue.Crc32 * multiplier), index);
        }

        foreach (var value in alternateValues.Keys) {
            nodeReplacements.TryGetValue(value, out var nodeReplacementsOfValue);
            foreach (var (selector, index) in oldSelectors) {
                if (!(nodeReplacementsOfValue?.TryGetValue(index, out var newIndex) ?? false)) {
                    newIndex = index;
                }

                shpk.RenderNodeSelectors.Add(unchecked(selector + value.Crc32 * multiplier), newIndex);
            }
        }

        Console.Error.WriteLine($"Added material key {key}");
    }

    private static void ConfigureResource(IndexedList<Name, ShaderResource> resources, string instruction)
    {
        var (name, type) = instruction.SplitOnce(':');
        var resource = resources[name];
        resource.Slot = unchecked((ushort)short.Parse(type ?? string.Empty));

        Console.Error.WriteLine($"Configured resource {name}");
    }

    private static void ReplaceOrAddShader(ShaderPackage shpk, string shaderId, string fileName)
    {
        var (programType, index) = ShaderPackageExtract.ParseShaderId(shaderId);
        var shader = ReadShader(fileName);
        var shaders = shpk.GetShadersByProgramType(programType);
        if (index == shaders.Count) {
            shaders.Add(shader);
            Console.Error.WriteLine($"Added shader {shaderId}");
        } else if (index < shaders.Count) {
            shaders[(int)index] = shader;
            Console.Error.WriteLine($"Replaced shader {shaderId}");
        } else {
            throw new ArgumentOutOfRangeException($"Invalid {programType} index {index} (valid range: 0..{shaders.Count} inclusive)");
        }
    }

    private static Shader ReadShader(string fileName)
    {
        var fileBytes = File.ReadAllBytes(fileName);
        var magic = MemoryMarshal.Read<InlineByteString<uint>>(fileBytes);
        if (magic == "DXBC"u8) {
            return Shader.FromDirectX11ShaderBlob(fileBytes);
        } else if (magic == "ShCd"u8) {
            return Shader.FromShaderCodeBytes(fileBytes);
        } else {
            throw new InvalidDataException($"Unrecognized magic number {magic}");
        }
    }
}
