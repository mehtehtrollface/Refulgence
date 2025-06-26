using Refulgence.IO;
using Refulgence.Text;
using Refulgence.Xiv.ShaderPackages;

namespace Refulgence.Xiv.IO;

internal static partial class ShaderReaderWriter
{
    private sealed class PackageWriter : Writer
    {
        private readonly Dictionary<uint, int> Aliases;
        private readonly ShaderPackage         ShaderPackage;

        public PackageWriter(ShaderPackage shaderPackage, Stream destination) : base(destination)
        {
            ShaderPackage = shaderPackage;
            Aliases = new(shaderPackage.RenderNodeSelectors);
            foreach (var primarySelector in shaderPackage.RenderNodes.Keys) {
                Aliases.Remove(primarySelector);
            }
        }

        protected override InlineByteString<uint> Magic
            => new("ShPk"u8);

        protected override uint Version
            => 0x0D01u;

        protected override GraphicsPlatform GraphicsPlatform
            => ShaderPackage.GraphicsPlatform;

        protected override void DoWrite()
        {
            Destination.Write(
                new PackageHeader13
                {
                    Super = new PackageHeader11
                    {
                        VertexShaderCount = (uint)ShaderPackage.VertexShaders.Count,
                        PixelShaderCount = (uint)ShaderPackage.PixelShaders.Count,
                        MaterialParametersSize = ShaderPackage.MaterialParametersSize,
                        MaterialParameterCount = (ushort)ShaderPackage.MaterialParameters.Count,
                        HasMaterialParamDefaults = (ushort)(ShaderPackage.MaterialParametersDefaults != null ? 1 : 0),
                        ConstantCount = (uint)ShaderPackage.ConstantBuffers.Count,
                        SamplerCount = (ushort)ShaderPackage.Samplers.Count,
                        TextureCount = (ushort)ShaderPackage.Textures.Count,
                        UavCount = (uint)ShaderPackage.Uavs.Count,
                        SystemKeyCount = (uint)ShaderPackage.SystemKeys.Count,
                        SceneKeyCount = (uint)ShaderPackage.SceneKeys.Count,
                        MaterialKeyCount = (uint)ShaderPackage.MaterialKeys.Count,
                        NodeCount = (uint)ShaderPackage.RenderNodes.Count,
                        NodeAliasCount = (uint)Aliases.Count,
                    },
                    UnkA = 0u,
                    UnkB = 0u,
                    UnkC = 0u,
                }
            );

            WriteShaders(ShaderPackage.VertexShaders, 1u);
            WriteShaders(ShaderPackage.PixelShaders,  4u);

            foreach (var materialParameter in ShaderPackage.MaterialParameters) {
                Destination.Write(
                    new MaterialParameter11
                    {
                        Crc32 = materialParameter.Name.Crc32,
                        ByteOffset = materialParameter.ByteOffset,
                        ByteSize = materialParameter.ByteSize,
                    }
                );
            }

            if (ShaderPackage.MaterialParametersDefaults != null) {
                if (ShaderPackage.MaterialParametersDefaults.Length >= ShaderPackage.MaterialParametersSize) {
                    Destination.Write(ShaderPackage.MaterialParametersDefaults, 0, (int)ShaderPackage.MaterialParametersSize);
                } else {
                    Destination.Write(ShaderPackage.MaterialParametersDefaults, 0, ShaderPackage.MaterialParametersDefaults.Length);
                    Destination.Write(
                        new byte[ShaderPackage.MaterialParametersSize - ShaderPackage.MaterialParametersDefaults.Length]
                    );
                }
            }

            WriteResources(ShaderPackage.ConstantBuffers);
            WriteResources(ShaderPackage.Samplers);
            WriteResources(ShaderPackage.Textures);
            WriteResources(ShaderPackage.Uavs);

            WriteShaderKeys(ShaderPackage.SystemKeys);
            WriteShaderKeys(ShaderPackage.SceneKeys);
            WriteShaderKeys(ShaderPackage.MaterialKeys);

            Destination.Write(ShaderPackage.SubViewKey0.DefaultValue.Crc32);
            Destination.Write(ShaderPackage.SubViewKey1.DefaultValue.Crc32);

            WriteRenderNodes();

            foreach (var (selector, index) in Aliases) {
                Destination.Write(
                    new NodeAlias11
                    {
                        Selector = selector,
                        NodeIndex = index,
                    }
                );
            }
        }

        private void WriteShaderKeys(IEnumerable<ShaderKey> keys)
        {
            foreach (var key in keys) {
                Destination.Write(
                    new ShaderKey11
                    {
                        KeyCrc32 = key.Key.Crc32,
                        DefaultValueCrc32 = key.DefaultValue.Crc32,
                    }
                );
            }
        }

        private void WriteRenderNodes()
        {
            foreach (var node in ShaderPackage.RenderNodes) {
                Destination.Write(
                    new NodeHeader11
                    {
                        PrimarySelector = node.PrimarySelector,
                        PassCount = (uint)node.Passes.Count,
                    }
                );

                Destination.Write(node.PassIndices);

                Destination.Write(node.SubViewValue0.Crc32);
                Destination.Write(node.SubViewValue1.Crc32);

                foreach (var key in ShaderPackage.SystemKeys) {
                    Destination.Write(node.SystemValues[key.Key].Crc32);
                }

                foreach (var key in ShaderPackage.SceneKeys) {
                    Destination.Write(node.SceneValues[key.Key].Crc32);
                }

                foreach (var key in ShaderPackage.MaterialKeys) {
                    Destination.Write(node.MaterialValues[key.Key].Crc32);
                }

                Destination.Write(node.SubViewValue0.Crc32);
                Destination.Write(node.SubViewValue1.Crc32);

                foreach (var pass in node.Passes) {
                    Destination.Write(
                        new RenderPass13
                        {
                            Super = new RenderPass11
                            {
                                NameCrc32 = pass.Name.Crc32,
                                VertexShaderIndex = pass.VertexShaderIndex,
                                PixelShaderIndex = pass.PixelShaderIndex,
                            },
                            UnkG = -1,
                            UnkH = -1,
                            UnkI = -1,
                        }
                    );
                }
            }
        }
    }
}
