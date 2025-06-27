using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using Refulgence.Cli.IO;
using Refulgence.Collections;
using Refulgence.Dxbc;
using Refulgence.Text;
using Refulgence.Xiv;
using Refulgence.Xiv.ShaderPackages;
using ProgramType = Refulgence.Xiv.ProgramType;

namespace Refulgence.Cli.Programs;

public static class Dump
{
    public static int Run(string inputFileName)
    {
        var fileBytes = File.ReadAllBytes(inputFileName);
        var magic = MemoryMarshal.Read<InlineByteString<uint>>(fileBytes);
        if (magic == "DXBC"u8) {
            DxContainer.FromBytes(fileBytes).Dump(Console.Out);
        } else if (magic == "ShCd"u8) {
            DumpShaderCode(Shader.FromShaderCodeBytes(fileBytes));
        } else if (magic == "ShPk"u8) {
            DumpShaderPackage(ShaderPackage.FromShaderPackageBytes(fileBytes));
        } else {
            throw new InvalidDataException($"Unrecognized magic number {magic}");
        }

        return 0;
    }

    private static void DumpShaderCode(Shader shcd)
    {
        Console.WriteLine($"ShCd for a {shcd.GraphicsPlatform} {shcd.ProgramType}");

        if (shcd.Textures.Count > 0 || shcd.Samplers.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("Textures and samplers used in this shader:");
            DumpTextures(shcd.Textures, shcd.Samplers);
        }

        if (shcd.Uavs.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("Unordered access views used in this shader:");
            DumpResources(shcd.Uavs);
        }

        if (shcd.ConstantBuffers.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("Constant buffers used in this shader:");
            DumpResources(shcd.ConstantBuffers);
        }

        if (shcd.ProgramType is ProgramType.VertexShader) {
            var inputs = MemoryMarshal.Cast<byte, VertexInput>(shcd.AdditionalHeader);
            Console.WriteLine();
            Console.WriteLine($"Vertex inputs declared in this shader: {inputs[0]}");
            if (inputs.Length > 1) {
                Console.WriteLine($"Vertex inputs used in this shader:     {inputs[1]}");
            }
        }
    }

    private static void DumpShaderPackage(ShaderPackage shpk)
    {
        Console.WriteLine($"ShPk for {shpk.GraphicsPlatform}");

        Console.WriteLine();
        Console.WriteLine("Valid shader IDs in this ShPk:");
        foreach (var (programType, shaders) in shpk.GetShaders()) {
            Console.WriteLine($"    {programType.ToAbbreviation()}0 .. {programType.ToAbbreviation()}{shaders.Count - 1} (inclusive)");
        }

        if (shpk.MaterialParameters.Count > 0 || shpk.MaterialParametersSize > 0) {
            Console.WriteLine();
            Console.WriteLine(
                $"Material parameters for this ShPk: {shpk.MaterialParametersSize >> 4} registers ({shpk.MaterialParametersSize} bytes)"
            );
            if (shpk.MaterialParametersDefaults is not null) {
                Console.WriteLine("    CRC32    Name                           Start  Size Default");
                Console.WriteLine("    -------- ------------------------------ ----- ----- ------------------------");
                foreach (var param in shpk.MaterialParameters) {
                    var defaults = shpk.MaterialParametersDefaults.AsSpan(param.ByteOffset, param.ByteSize);
                    var strDefaults = new StringBuilder();
                    var first = true;
                    foreach (var component in MemoryMarshal.Cast<byte, uint>(defaults)) {
                        if (first) {
                            first = false;
                        } else {
                            strDefaults.Append(", ");
                        }

                        strDefaults.AppendImmediateToString(component, null);
                    }

                    Console.WriteLine(
                        $"    {param.Name.Crc32:X8} {param.Name.Value,-30} {param.ByteOffset,5} {param.ByteSize,5} {strDefaults}"
                    );
                }
            } else {
                Console.WriteLine("    CRC32    Name                           Start  Size");
                Console.WriteLine("    -------- ------------------------------ ----- -----");
                foreach (var param in shpk.MaterialParameters) {
                    Console.WriteLine($"    {param.Name.Crc32:X8} {param.Name.Value,-30} {param.ByteOffset,5} {param.ByteSize,5}");
                }
            }
        }

        if (shpk.Textures.Count > 0 || shpk.Samplers.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("Textures and samplers used in this ShPk:");
            DumpTextures(shpk.Textures, shpk.Samplers);
        }

        if (shpk.Uavs.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("Unordered access views used in this ShPk:");
            DumpResources(shpk.Uavs);
        }

        if (shpk.ConstantBuffers.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("Constant buffers used in this ShPk:");
            DumpResources(shpk.ConstantBuffers);
        }

        if (shpk.SystemKeys.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("System keys used in this ShPk:");
            DumpShaderKeys(shpk.SystemKeys);
        }

        if (shpk.SceneKeys.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("Scene keys used in this ShPk:");
            DumpShaderKeys(shpk.SceneKeys);
        }

        if (shpk.MaterialKeys.Count > 0) {
            Console.WriteLine();
            Console.WriteLine("Material keys used in this ShPk:");
            DumpShaderKeys(shpk.MaterialKeys);
        }

        Console.WriteLine();
        Console.WriteLine("Sub-view keys used in this ShPk:");
        Console.WriteLine("    Index V. CRC32 Value Name");
        Console.WriteLine("    ----- -------- ---------------------------");
        DumpShaderKey(0, shpk.SubViewKey0);
        DumpShaderKey(1, shpk.SubViewKey1);

        var passes = new HashSet<Name>();
        foreach (var node in shpk.RenderNodes) {
            node.Passes.Keys.AddTo(passes);
        }

        Console.WriteLine();
        Console.WriteLine("Render passes done by this ShPk:");
        Console.WriteLine("    CRC32    Name");
        Console.WriteLine("    -------- -----------------------------------------------");
        foreach (var pass in passes) {
            Console.WriteLine($"    {pass.Crc32:X8} {pass.Value}");
        }
    }

    private static void DumpTextures(IndexedList<Name, ShaderResource> textures, IndexedList<Name, ShaderResource> samplers)
    {
        Console.WriteLine("    CRC32    Name                           TType TSlot TSize SSlot SSize");
        Console.WriteLine("    -------- ------------------------------ ----- ----- ----- ----- -----");
        foreach (var texture in textures) {
            if (samplers.TryGetValue(texture.Name, out var sampler)) {
                Console.WriteLine(
                    $"    {texture.Name.Crc32:X8} {texture.Name.Value,-30} {unchecked((short)texture.Type),5} {unchecked((short)texture.Slot),5} {unchecked((short)texture.Size),5} {unchecked((short)sampler.Slot),5} {unchecked((short)sampler.Size),5}"
                );
            } else {
                Console.WriteLine(
                    $"    {texture.Name.Crc32:X8} {texture.Name.Value,-30} {unchecked((short)texture.Type),5} {unchecked((short)texture.Slot),5} {unchecked((short)texture.Size),5}     -     -"
                );
            }
        }

        foreach (var sampler in samplers) {
            if (!textures.ContainsKey(sampler.Name)) {
                Console.WriteLine(
                    $"    {sampler.Name.Crc32:X8} {sampler.Name.Value,-30}     -     -     - {unchecked((short)sampler.Slot),5} {unchecked((short)sampler.Size),5}"
                );
            }
        }
    }

    private static void DumpResources(IEnumerable<ShaderResource> resources)
    {
        Console.WriteLine("    CRC32    Name                           Slot Size");
        Console.WriteLine("    -------- ------------------------------ ---- ----");
        foreach (var resource in resources) {
            Console.WriteLine(
                $"    {resource.Name.Crc32:X8} {resource.Name.Value,-30} {unchecked((short)resource.Slot),4} {unchecked((short)resource.Size),4}"
            );
        }
    }

    private static void DumpShaderKeys(IEnumerable<ShaderKey> keys)
    {
        Console.WriteLine("    K. CRC32 Key Name                       V. CRC32 Value Name");
        Console.WriteLine("    -------- ------------------------------ -------- ---------------------------");
        foreach (var key in keys) {
            Console.WriteLine($"    {key.Key.Crc32:X8} {key.Key.Value,-30} {key.DefaultValue.Crc32:X8} {key.DefaultValue.Value}");
            foreach (var value in key.Values) {
                if (value != key.DefaultValue) {
                    Console.WriteLine($"                                            {value.Crc32:X8} {value.Value}");
                }
            }
        }
    }

    private static void DumpShaderKey(int index, ShaderKey key)
    {
        Console.WriteLine($"    {index,5} {key.DefaultValue.Crc32:X8} {key.DefaultValue.Value}");
        foreach (var value in key.Values) {
            if (value != key.DefaultValue) {
                Console.WriteLine($"          {value.Crc32:X8} {value.Value}");
            }
        }
    }
}
