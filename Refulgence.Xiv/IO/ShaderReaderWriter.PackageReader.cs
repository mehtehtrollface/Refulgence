using System.Runtime.InteropServices;
using Refulgence.Collections;
using Refulgence.Xiv.ShaderPackages;

namespace Refulgence.Xiv.IO;

internal static partial class ShaderReaderWriter
{
    private class Package11Reader
    {
        public virtual void Read(ShaderPackage result, ref Reader reader)
            => DoRead11(result, ref reader, in reader.Header.ReadRef<PackageHeader11>());

        protected void DoRead11(ShaderPackage result, ref Reader reader, in PackageHeader11 header)
        {
            ReadShaders(result.VertexShaders, ref reader, (int)header.VertexShaderCount, ProgramType.VertexShader);
            ReadShaders(result.PixelShaders, ref reader,  (int)header.PixelShaderCount,  ProgramType.PixelShader);

            result.MaterialParametersSize = header.MaterialParametersSize;
            reader.Header.Read<MaterialParameter11>(header.MaterialParameterCount)
                  .AddTo(
                       result.MaterialParameters,
                       param => new(Names.KnownNames.TryResolve(param.Crc32), param.ByteOffset, param.ByteSize)
                   );
            if (header.HasMaterialParamDefaults != 0) {
                result.MaterialParametersDefaults = reader.Header.Read<byte>((int)header.MaterialParametersSize).ToArray();
            }

            ReadResources(result.ConstantBuffers, ref reader, (int)header.ConstantCount);
            ReadResources(result.Samplers,  ref reader, header.SamplerCount);
            ReadResources(result.Textures,  ref reader, header.TextureCount);
            ReadResources(result.Uavs,      ref reader, (int)header.UavCount);

            var keyValueResolvers = new Dictionary<ShaderKey, IReadOnlyDictionary<uint, Name>>();
            reader.Header.Read<ShaderKey11>((int)header.SystemKeyCount)
                  .AddTo(result.SystemKeys, key => DecodeKey(key, keyValueResolvers));
            reader.Header.Read<ShaderKey11>((int)header.SceneKeyCount)
                  .AddTo(result.SceneKeys, key => DecodeKey(key, keyValueResolvers));
            reader.Header.Read<ShaderKey11>((int)header.MaterialKeyCount)
                  .AddTo(result.MaterialKeys, key => DecodeKey(key, keyValueResolvers));

            result.SubViewKey0 = new(Name.Empty, Names.KnownNames.TryResolve(reader.Header.Read<uint>()));
            result.SubViewKey1 = new(Name.Empty, Names.KnownNames.TryResolve(reader.Header.Read<uint>()));

            ReadRenderNodes11(result, ref reader, (int)header.NodeCount, keyValueResolvers);
            foreach (var alias in reader.Header.Read<NodeAlias11>((int)header.NodeAliasCount)) {
                result.RenderNodeSelectors.Add(alias.Selector, alias.NodeIndex);
            }
        }

        protected void ReadShaders(List<Shader> result, ref Reader reader, int count, ProgramType programType)
        {
            result.EnsureCapacity(result.Count + count);
            for (var i = 0; i < count; ++i) {
                var shader = new Shader(reader.CommonHeader.GraphicsPlatform, programType);
                ReadShader(shader, ref reader);
                result.Add(shader);
            }
        }

        protected virtual void ReadShader(Shader shader, ref Reader reader)
            => ReadCode5(shader, ref reader);

        private void ReadRenderNodes11(ShaderPackage result, ref Reader reader, int count,
            Dictionary<ShaderKey, IReadOnlyDictionary<uint, Name>> keyValueResolvers)
        {
            for (var i = 0; i < count; ++i) {
                var header = reader.Header.Read<NodeHeader11>();

                var node = new RenderNode(header.PrimarySelector);

                reader.Header.Read<byte>(16).CopyTo(node.PassIndices);

                ReadRenderNodeValues(node, ref reader, result, keyValueResolvers);
                ReadRenderNodePasses(node.Passes, ref reader, (int)header.PassCount);

                result.RenderNodes.Add(node);
                result.RenderNodeSelectors.Add(node.PrimarySelector, i);
            }
        }

        protected virtual void ReadRenderNodeValues(RenderNode result, ref Reader reader, ShaderPackage package,
            Dictionary<ShaderKey, IReadOnlyDictionary<uint, Name>> keyValueResolvers)
        {
            ReadValues(result.SystemValues,   ref reader, package.SystemKeys,   keyValueResolvers);
            ReadValues(result.SceneValues,    ref reader, package.SceneKeys,    keyValueResolvers);
            ReadValues(result.MaterialValues, ref reader, package.MaterialKeys, keyValueResolvers);

            result.SubViewValue0 = ReadValue(ref reader, package.SubViewKey0, null);
            result.SubViewValue1 = ReadValue(ref reader, package.SubViewKey1, null);
        }

        protected virtual void ReadRenderNodePasses(IndexedList<Name, RenderPass> result, ref Reader reader, int count)
        {
            reader.Header.Read<RenderPass11>(count)
                  .AddTo(result, DecodePass11);
        }

        protected static RenderPass DecodePass11(in RenderPass11 pass)
            => new(Names.KnownNames.TryResolve(pass.NameCrc32), pass.VertexShaderIndex, pass.PixelShaderIndex);

        protected static ShaderKey DecodeKey(ShaderKey11 key,
            Dictionary<ShaderKey, IReadOnlyDictionary<uint, Name>> keyValueResolvers)
        {
            var keyName = Names.KnownNames.TryResolve(key.KeyCrc32);
            var resolver = keyName.WithKnownSuffixes();
            var decodedKey = new ShaderKey(keyName, resolver.TryResolve(Names.KnownNames, key.DefaultValueCrc32));
            keyValueResolvers.Add(decodedKey, resolver);
            return decodedKey;
        }

        protected static void ReadValues(Dictionary<Name, Name> values, ref Reader reader, IEnumerable<ShaderKey> keys,
            Dictionary<ShaderKey, IReadOnlyDictionary<uint, Name>> keyValueResolvers)
        {
            foreach (var key in keys) {
                values.Add(key.Key, ReadValue(ref reader, key, keyValueResolvers[key]));
            }
        }

        protected static Name ReadValue(ref Reader reader, ShaderKey key, IReadOnlyDictionary<uint, Name>? valueResolver)
        {
            var value = valueResolver.TryResolve(Names.KnownNames, reader.Header.Read<uint>());
            key.Values.Add(value);
            return value;
        }
    }

    private class Package13Reader : Package11Reader
    {
        public override void Read(ShaderPackage result, ref Reader reader)
            => DoRead13(result, ref reader, in reader.Header.ReadRef<PackageHeader13>());

        protected void DoRead13(ShaderPackage result, ref Reader reader, in PackageHeader13 header)
        {
            DoRead11(result, ref reader, in header.Super);
            if (header.UnkA != 0u) {
                throw new NotImplementedException(
                    $"{nameof(Package13Reader)}.{nameof(DoRead13)}: unhandled UnkA {header.UnkA:X}"
                );
            }

            if (header.UnkB != 0u) {
                throw new NotImplementedException(
                    $"{nameof(Package13Reader)}.{nameof(DoRead13)}: unhandled UnkB {header.UnkB:X}"
                );
            }

            if (header.UnkC != 0u) {
                throw new NotImplementedException(
                    $"{nameof(Package13Reader)}.{nameof(DoRead13)}: unhandled UnkC {header.UnkC:X}"
                );
            }
        }

        protected override void ReadShader(Shader shader, ref Reader reader)
            => ReadCode6(
                shader, ref reader, shader.ProgramType switch
                {
                    ProgramType.VertexShader => 1u,
                    ProgramType.PixelShader  => 4u,
                    _ => throw new NotImplementedException(
                        $"{nameof(Package13Reader)}.{nameof(ReadShader)}: unimplemented program type {shader.ProgramType}"
                    ),
                }
            );

        protected override void ReadRenderNodeValues(RenderNode result, ref Reader reader, ShaderPackage package,
            Dictionary<ShaderKey, IReadOnlyDictionary<uint, Name>> keyValueResolvers)
        {
            var unkE = reader.Header.Read<uint>();
            var unkF = reader.Header.Read<uint>();
            base.ReadRenderNodeValues(result, ref reader, package, keyValueResolvers);
            if (unkE != result.SubViewValue0.Crc32) {
                throw new NotImplementedException(
                    $"{nameof(Package13Reader)}.{nameof(ReadRenderNodeValues)}: unhandled UnkE {unkE:X8}, expected {result.SubViewValue0.Crc32:X8}"
                );
            }

            if (unkF != result.SubViewValue1.Crc32) {
                throw new NotImplementedException(
                    $"{nameof(Package13Reader)}.{nameof(ReadRenderNodeValues)}: unhandled UnkE {unkF:X8}, expected {result.SubViewValue1.Crc32:X8}"
                );
            }
        }

        protected override void ReadRenderNodePasses(IndexedList<Name, RenderPass> result, ref Reader reader, int count)
        {
            reader.Header.Read<RenderPass13>(count)
                  .AddTo(result, DecodePass13);
        }

        protected static RenderPass DecodePass13(in RenderPass13 pass)
        {
            if (pass.UnkG != -1) {
                throw new NotImplementedException($"{nameof(Package13Reader)}.{nameof(DecodePass13)}: unhandled UnkG {pass.UnkG}");
            }

            if (pass.UnkH != -1) {
                throw new NotImplementedException($"{nameof(Package13Reader)}.{nameof(DecodePass13)}: unhandled UnkH {pass.UnkH}");
            }

            if (pass.UnkI != -1) {
                throw new NotImplementedException($"{nameof(Package13Reader)}.{nameof(DecodePass13)}: unhandled UnkI {pass.UnkI}");
            }

            return DecodePass11(in pass.Super);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PackageHeader11
    {
        public uint   VertexShaderCount;
        public uint   PixelShaderCount;
        public uint   MaterialParametersSize;
        public ushort MaterialParameterCount;
        public ushort HasMaterialParamDefaults;
        public uint   ConstantCount;
        public ushort SamplerCount;
        public ushort TextureCount;
        public uint   UavCount;
        public uint   SystemKeyCount;
        public uint   SceneKeyCount;
        public uint   MaterialKeyCount;
        public uint   NodeCount;
        public uint   NodeAliasCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PackageHeader13
    {
        public PackageHeader11 Super;
        public uint            UnkA;
        public uint            UnkB;
        public uint            UnkC;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MaterialParameter11
    {
        public uint   Crc32;
        public ushort ByteOffset;
        public ushort ByteSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderKey11
    {
        public uint KeyCrc32;
        public uint DefaultValueCrc32;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NodeHeader11
    {
        public uint PrimarySelector;
        public uint PassCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RenderPass11
    {
        public uint NameCrc32;
        public int  VertexShaderIndex;
        public int  PixelShaderIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RenderPass13
    {
        public RenderPass11 Super;
        public int          UnkG;
        public int          UnkH;
        public int          UnkI;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NodeAlias11
    {
        public uint Selector;
        public int  NodeIndex;
    }
}
