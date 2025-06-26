using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Refulgence.Cli.IO;
using Refulgence.Collections;
using Refulgence.Dxbc;
using Refulgence.IO;
using Refulgence.Text;
using Refulgence.Xiv;
using Refulgence.Xiv.ShaderPackages;
using ProgramType = Refulgence.Xiv.ProgramType;

namespace Refulgence.Cli.Programs;

public static class RoundTripTest
{
    public static int Run(string inputFileName, string outputFileName)
    {
        using var mmio = MmioMemoryManager.CreateFromFile(inputFileName, access: MemoryMappedFileAccess.Read);
        var mmioSpan = (ReadOnlySpan<byte>)mmio.GetSpan();
        var magic = MemoryMarshal.Read<InlineByteString<uint>>(mmioSpan);
        byte[] reconstructed;
        if (magic == "DXBC"u8) {
            reconstructed = TestDxContainer(mmioSpan);
        } else if (magic == "ShCd"u8) {
            reconstructed = TestShaderCode(mmioSpan);
        } else if (magic == "ShPk"u8) {
            reconstructed = TestShaderPackage(mmioSpan);
        } else {
            throw new InvalidDataException($"Unrecognized magic number {magic}");
        }

        File.WriteAllBytes(outputFileName, reconstructed);

        return mmioSpan.SequenceEqual(reconstructed) ? 0 : 1;
    }

    private static byte[] TestDxContainer(ReadOnlySpan<byte> span)
    {
        var container = DxContainer.FromBytes(span, opaque: true);
        AssertEqual("DxContainer", ((IBytesConvertible)container).ToBytes(), span);

        var reconstructedContainer = new DxContainer();
        foreach (var (type, part) in container.Parts) {
            if (part is not OpaqueDxPart) {
                reconstructedContainer.Parts.Add(type, part);
                continue;
            }

            var partBytes = part.ToBytes();
            var parsedPart = DxPart.Create(type, partBytes);
            reconstructedContainer.Parts.Add(type, parsedPart);
            AssertEqual(type.ToString(), parsedPart.ToBytes(), partBytes);
        }

        var reconstructedBytes = ((IBytesConvertible)reconstructedContainer).ToBytes();
        AssertEqual("DxContainer deep", reconstructedBytes, span);

        return reconstructedBytes;
    }

    private static byte[] TestShaderCode(ReadOnlySpan<byte> span)
    {
        var shader = Shader.FromShaderCodeBytes(span);
        AssertEqual("ShCd", shader.ToShaderCodeBytes(), span);

        AssertEqual("ShCd reconstruction", Shader.FromDirectX11ShaderBlob(shader.ShaderBlob).ToShaderCodeBytes(), span);

        var reconstructedBytes = Shader.FromDirectX11ShaderBlob(TestDxContainer(shader.ShaderBlob)).ToShaderCodeBytes();
        AssertEqual("ShCd deep", reconstructedBytes, span);

        return reconstructedBytes;
    }

    private static byte[] TestShaderPackage(ReadOnlySpan<byte> span)
    {
        var package = ShaderPackage.FromShaderPackageBytes(span);
        AssertEqual("ShPk", package.ToShaderPackageBytes(), span);

        package.UpdateResources();
        AssertEqual("Resource update", package.ToShaderPackageBytes(), span);

        Console.Error.WriteLine("Inner ShCd reconstruction...");
        foreach (var (programType, shaders) in package.GetShaders()) {
            for (var i = 0; i < shaders.Count; ++i) {
                TestShader(shaders, programType, i);
            }
        }

        package.UpdateResources();
        var reconstructedBytes = package.ToShaderPackageBytes();
        AssertEqual("Inner ShCds reconstruction", reconstructedBytes, span);

        return reconstructedBytes;

        static void TestShader(List<Shader> shaders, ProgramType type, int index)
        {
            var before = shaders[index];
            var after = Shader.FromDirectX11ShaderBlob(before.ShaderBlob);
            shaders[index] = after;
            if (!AssertEqual($"    {type.ToAbbreviation()}{index}", after.ToShaderCodeBytes(), before.ToShaderCodeBytes())) {
                Console.Error.WriteLine();
                Console.Error.WriteLine("        Samplers:");
                DumpResources(before.Samplers, after.Samplers);
                Console.Error.WriteLine();
                Console.Error.WriteLine("        Textures:");
                DumpResources(before.Textures, after.Textures);
            }
        }

        static void DumpResources(IndexedList<Name, ShaderResource> before, IndexedList<Name, ShaderResource> after)
        {
            Console.Error.WriteLine("        CRC32    Name                           Type Slot Size | Type Slot Size");
            Console.Error.WriteLine("        -------- ------------------------------ ---- ---- ---- | ---- ---- ----");
            foreach (var resource in before) {
                if (after.TryGetValue(resource.Name, out var resAfter)) {
                    Console.Error.WriteLine(
                        $"        {resource.Name.Crc32:X8} {resource.Name,-30} {unchecked((short)resource.Type),4} {unchecked((short)resource.Slot),4} {unchecked((short)resource.Size),4} | {unchecked((short)resAfter.Type),4} {unchecked((short)resAfter.Slot),4} {unchecked((short)resAfter.Size),4}"
                    );
                } else {
                    Console.Error.WriteLine(
                        $"        {resource.Name.Crc32:X8} {resource.Name,-30} {unchecked((short)resource.Type),4} {unchecked((short)resource.Slot),4} {unchecked((short)resource.Size),4} |    -    -    -"
                    );
                }
            }

            foreach (var resource in after) {
                if (!before.ContainsKey(resource.Name)) {
                    Console.Error.WriteLine(
                        $"        {resource.Name.Crc32:X8} {resource.Name,-30}    -    -    - | {(ushort)resource.Type,4} {resource.Slot,4} {resource.Size,4}"
                    );
                }
            }
        }
    }

    private static bool AssertEqual(string description, ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected)
    {
        if (expected.SequenceEqual(actual)) {
            Console.Error.WriteLine($"{description} round-trip test passed");
            return true;
        } else {
            Console.Error.WriteLine(
                $"{description} round-trip test FAILED (actual: {actual.Length} bytes, expected: {expected.Length} bytes)"
            );
            return false;
        }
    }
}
