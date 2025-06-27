using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Refulgence.Collections;
using Refulgence.Dxbc;
using Refulgence.Dxbc.ResourceDefinition;
using Refulgence.Dxbc.Signature;
using Refulgence.IO;
using Refulgence.Sm5;
using Refulgence.Text;
using Refulgence.Xiv.IO;

namespace Refulgence.Xiv;

public sealed class Shader(GraphicsPlatform platform, ProgramType programType) : IBytesConvertible
{
    public readonly IndexedList<Name, ShaderResource> ConstantBuffers  = new(16, resource => resource.Name);
    public readonly GraphicsPlatform                  GraphicsPlatform = platform;
    public readonly IndexedList<Name, ShaderResource> Samplers         = new(16, resource => resource.Name);
    public readonly ProgramType                       ProgramType      = programType;
    public readonly IndexedList<Name, ShaderResource> Textures         = new(16, resource => resource.Name);
    public readonly IndexedList<Name, ShaderResource> Uavs             = new(16, resource => resource.Name);

    private static readonly ImmutableHashSet<Name> NumberedResources = ["g_SamplerGBuffer",];

    private static readonly ImmutableHashSet<Name> NumberedResourcesTransitive = ["g_SamplerNormal",];

    public byte[] AdditionalHeader =
        ArrayHelper.NewOrEmpty<byte>(ShaderReaderWriter.GetShaderExtraHeaderSize(platform, programType));

    public byte[] ShaderBlob = [];

    public static Shader FromShaderCodeBytes(ReadOnlySpan<byte> bytes)
        => ShaderReaderWriter.ReadCode(bytes);

    public static Shader FromDirectX11ShaderBlob(byte[] shaderBlob)
    {
        var container = DxContainer.FromBytes(shaderBlob);
        if (!container.TryGetResourceDefinition(out var rdef)) {
            throw new InvalidDataException("Missing or mistyped RDEF part");
        }

        var shader = new Shader(GraphicsPlatform.DirectX11, rdef.ProgramType.ToXivProgramType());
        shader.ShaderBlob = shaderBlob;
        shader.PopulateResources(rdef);

        if (shader.ProgramType is ProgramType.VertexShader) {
            if (!container.TryGetInputSignature(out var isgn)) {
                throw new InvalidDataException("Missing or mistyped ISGN part");
            }

            shader.PopulateVertexShaderInputs(isgn);
        }

        if (!container.TryGetShader(out var shdr)) {
            throw new InvalidDataException("Missing or mistyped SHDR/SHEX part");
        }

        shader.PopulateResourceAssociations(shdr);

        return shader;
    }

    private void PopulateResources(ResourceDefinitionDxPart rdef)
    {
        foreach (var bind in rdef.Bindings) {
            switch (bind.InputType) {
                case ShaderInputType.CBuffer:
                    ConstantBuffers.Add(
                        new(
                            bind.Name.SplitOnce('.').Before, ShaderResourceType.Undefined, (ushort)bind.BindPoint, (ushort)
                            ((rdef.ConstantBuffers[bind.Name].Size + 0xF) >> 4)
                        )
                    );
                    break;
                case ShaderInputType.TBuffer:
                case ShaderInputType.Texture:
                case ShaderInputType.Structured:
                case ShaderInputType.ByteAddress:
                    Textures.Add(
                        new(
                            bind.Name.SplitOnce('.').Before, ToShaderResourceType(bind.ViewDimension), (ushort)bind.BindPoint,
                            ushort.MaxValue
                        )
                    );
                    break;
                case ShaderInputType.Sampler:
                    Samplers.Add(
                        new(
                            bind.Name.SplitOnce('.').Before, ShaderResourceType.Undefined, (ushort)bind.BindPoint,
                            ushort.MaxValue
                        )
                    );
                    break;
                case ShaderInputType.UavRWTyped:
                case ShaderInputType.UavRWStructured:
                case ShaderInputType.UavRWByteAddress:
                case ShaderInputType.UavAppendStructured:
                case ShaderInputType.UavConsumeStructured:
                case ShaderInputType.UavRWStructuredWithCounter:
                    Uavs.Add(
                        new(
                            bind.Name.SplitOnce('.').Before, ToShaderResourceType(bind.ViewDimension), (ushort)bind.BindPoint,
                            (ushort)bind.BindCount
                        )
                    );
                    break;
            }
        }
    }

    private static ShaderResourceType ToShaderResourceType(ShaderResourceViewDimension dimension)
        => dimension switch
        {
            ShaderResourceViewDimension.Buffer   => ShaderResourceType.Buffer,
            ShaderResourceViewDimension.BufferEx => ShaderResourceType.Buffer,
            _                                    => ShaderResourceType.Texture,
        };

    private void PopulateVertexShaderInputs(SignatureDxPart isgn)
    {
        VertexInput declaredInputs = 0;
        VertexInput usedInputs = 0;
        foreach (var input in isgn.Elements) {
            var inputFlag = input.ToVertexInput();
            declaredInputs |= inputFlag;
            if (input.ReadWriteMask != 0) {
                usedInputs |= inputFlag;
            }
        }

        var inputs = MemoryMarshal.Cast<byte, VertexInput>(AdditionalHeader);
        if (inputs.Length > 0) {
            inputs[0] = declaredInputs;
        }

        if (inputs.Length > 1) {
            inputs[1] = usedInputs;
        }
    }

    private void PopulateResourceAssociations(ShaderDxPart shdr)
    {
        var textures =
            new SharedSet<uint, ulong>.SortedUniverse(Textures.Select((ShaderResource texture) => texture.Name.Crc32));
        var samplers =
            new SharedSet<uint, ulong>.SortedUniverse(Samplers.Select((ShaderResource texture) => texture.Name.Crc32));
        var textureSamplers = new Dictionary<uint, SharedSet<uint, ulong>>(Textures.Count);
        var samplerTextures = new Dictionary<uint, SharedSet<uint, ulong>>(Samplers.Count);
        foreach (var instruction in shdr.Instructions) {
            if (instruction.OpCode.Type.GetInfo().Flags.HasFlag(OpCodeFlags.CustomOperands)) {
                continue;
            }

            var currentSamplers = samplers.EmptySet();
            var currentTextures = textures.EmptySet();

            foreach (var operand in new OperandDecoder(instruction.Operands)) {
                if (operand.Header.IndexDimensions < 1
                 || operand.Header.Index0Representation != OperandIndexRepresentation.Immediate32) {
                    continue;
                }

                switch (operand.Header.Type) {
                    case OperandType.Resource:
                        currentTextures.AddExisting(Textures[(int)operand.Index0[0]].Name.Crc32);
                        break;
                    case OperandType.Sampler:
                        currentSamplers.AddExisting(Samplers[(int)operand.Index0[0]].Name.Crc32);
                        break;
                }
            }

            if (currentSamplers.Count > 0 && currentTextures.Count > 0) {
                foreach (var sampler in currentSamplers) {
                    Add(samplerTextures, sampler, currentTextures);
                }

                foreach (var texture in currentTextures) {
                    Add(textureSamplers, texture, currentSamplers);
                }
            }
        }

        NumberResources(Samplers, samplerTextures);
        NumberResources(Textures, textureSamplers);

        return;

        static void Add(Dictionary<uint, SharedSet<uint, ulong>> matrix, uint key, SharedSet<uint, ulong> values)
        {
            if (matrix.TryGetValue(key, out var currentValues)) {
                matrix[key] = currentValues | values;
            } else {
                matrix.Add(key, values);
            }
        }

        static void NumberResources(IndexedList<Name, ShaderResource> resources, Dictionary<uint, SharedSet<uint, ulong>> matrix)
        {
            ushort next = 0;
            foreach (var resource in resources) {
                var ownId = resource.Name.Crc32;
                matrix.TryGetValue(ownId, out var otherIds);
                if (otherIds.Contains(ownId) || NumberedResources.Contains(ownId) || NumberedResourcesTransitive.Contains(ownId)) {
                    resource.Size = next++;
                    continue;
                }

                foreach (var otherId in otherIds) {
                    if (NumberedResourcesTransitive.Contains(otherId)) {
                        resource.Size = next++;
                        break;
                    }
                }
            }
        }
    }

    public byte[] ToShaderCodeBytes()
    {
        using var buffer = new MemoryStream();
        ShaderReaderWriter.Write(this, buffer);

        return buffer.ToArray();
    }

    public void WriteShaderCodeTo(Stream destination)
        => ShaderReaderWriter.Write(this, destination);

    void IBytesConvertible.WriteTo(Stream destination)
        => ShaderReaderWriter.Write(this, destination);
}
