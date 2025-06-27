using System.Diagnostics.CodeAnalysis;
using Refulgence.Collections;
using Refulgence.Dxbc.ResourceDefinition;
using Refulgence.IO;
using Refulgence.Xiv.IO;

namespace Refulgence.Xiv.ShaderPackages;

public sealed class ShaderPackage(GraphicsPlatform graphicsPlatform) : IBytesConvertible
{
    public const uint SelectorMultiplier        = 31;
    public const uint SelectorInverseMultiplier = 3186588639; // 31 * 3186588639 = 23 << 32 + 1, iow they're modular inverses

    public static readonly Name MaterialParametersConstantName = "g_MaterialParameter";
    public static readonly Name TableSamplerName               = "g_SamplerTable";
    public static readonly Name NormalSamplerName              = "g_SamplerNormal";
    public static readonly Name IndexSamplerName               = "g_SamplerIndex";

    public readonly GraphicsPlatform GraphicsPlatform           = graphicsPlatform;
    public          uint             MaterialParametersSize     = 0;
    public          byte[]?          MaterialParametersDefaults = null;

    public readonly List<Shader> PixelShaders  = new(16);
    public readonly List<Shader> VertexShaders = new(16);

    public readonly IndexedList<Name, ShaderResource> ConstantBuffers = new(16, resource => resource.Name);
    public readonly IndexedList<Name, ShaderResource> Samplers        = new(16, resource => resource.Name);
    public readonly IndexedList<Name, ShaderResource> Textures        = new(16, resource => resource.Name);
    public readonly IndexedList<Name, ShaderResource> Uavs            = new(16, resource => resource.Name);

    public readonly IndexedList<Name, MaterialParameter> MaterialParameters = new(16, parameter => parameter.Name);

    public readonly IndexedList<Name, ShaderKey> SystemKeys   = new(16, key => key.Key);
    public readonly IndexedList<Name, ShaderKey> SceneKeys    = new(16, key => key.Key);
    public readonly IndexedList<Name, ShaderKey> MaterialKeys = new(16, key => key.Key);
    public          ShaderKey                    SubViewKey0  = new(Name.Empty, Name.Empty);
    public          ShaderKey                    SubViewKey1  = new(Name.Empty, Name.Empty);

    public readonly IndexedList<uint, RenderNode> RenderNodes         = new(16, node => node.PrimarySelector);
    public readonly OrderedDictionary<uint, int>  RenderNodeSelectors = new(128);

    public IEnumerable<(ProgramType ProgramType, List<Shader> Shaders)> GetShaders()
    {
        yield return (ProgramType.VertexShader, VertexShaders);
        yield return (ProgramType.PixelShader, PixelShaders);
    }

    public List<Shader> GetShadersByProgramType(ProgramType programType)
        => programType switch
        {
            ProgramType.VertexShader => VertexShaders,
            ProgramType.PixelShader => PixelShaders,
            _ => throw new ArgumentException($"{nameof(GetShadersByProgramType)}: Unsupported program type {programType}"),
        };

    public bool TryGetRenderNode(uint selector, [NotNullWhen(true)] out RenderNode? node)
    {
        if (RenderNodes.TryGetValue(selector, out node)) {
            return true;
        }

        if (RenderNodeSelectors.TryGetValue(selector, out var index) && index < RenderNodes.Count) {
            node = RenderNodes[index];
            return true;
        }

        node = null;
        return false;
    }

    public void UpdateResources()
    {
        var constants = new Dictionary<Name, ShaderResource>();
        var samplers = new Dictionary<Name, ShaderResource>();
        var textures = new Dictionary<Name, ShaderResource>();
        var uavs = new Dictionary<Name, ShaderResource>();

        foreach (var shader in VertexShaders) {
            CollectResources(constants, shader.ConstantBuffers, ConstantBuffers, ShaderInputType.CBuffer);
            CollectResources(samplers,  shader.Samplers,        Samplers,        ShaderInputType.Sampler);
            CollectResources(textures,  shader.Textures,        Textures,        ShaderInputType.Texture);
            CollectResources(uavs,      shader.Uavs,            Uavs,            ShaderInputType.UavRWTyped);
        }

        foreach (var shader in PixelShaders) {
            CollectResources(constants, shader.ConstantBuffers, ConstantBuffers, ShaderInputType.CBuffer);
            CollectResources(samplers,  shader.Samplers,        Samplers,        ShaderInputType.Sampler);
            CollectResources(textures,  shader.Textures,        Textures,        ShaderInputType.Texture);
            CollectResources(uavs,      shader.Uavs,            Uavs,            ShaderInputType.UavRWTyped);
        }

        ConstantBuffers.Clear();
        constants.Values.AddTo(ConstantBuffers);
        Samplers.Clear();
        samplers.Values.AddTo(Samplers);
        Textures.Clear();
        textures.Values.AddTo(Textures);
        Uavs.Clear();
        uavs.Values.AddTo(Uavs);

        // Ceil required size to a multiple of 16 bytes.
        // Offsets can be skipped, MaterialParamsConstantId's size is the count.
        MaterialParametersSize = ConstantBuffers.TryGetValue(MaterialParametersConstantName, out var mpConstant)
            ? (uint)mpConstant.Size << 4
            : 0u;
        foreach (var param in MaterialParameters) {
            MaterialParametersSize = Math.Max(MaterialParametersSize, (uint)param.ByteOffset + param.ByteSize);
        }

        MaterialParametersSize = (MaterialParametersSize + 0xFu) & ~0xFu;

        // Automatically grow MaterialParametersDefaults if needed. Shrinking it will be handled at write time.
        if (MaterialParametersDefaults != null && MaterialParametersDefaults.Length < MaterialParametersSize) {
            var newDefaults = new byte[MaterialParametersSize];
            Array.Copy(MaterialParametersDefaults, newDefaults, MaterialParametersDefaults.Length);
            MaterialParametersDefaults = newDefaults;
        }
    }

    private static void CollectResources(Dictionary<Name, ShaderResource> resources, IEnumerable<ShaderResource> shaderResources,
        IndexedList<Name, ShaderResource> existingResources, ShaderInputType type)
    {
        foreach (var resource in shaderResources) {
            if (resources.TryGetValue(resource.Name, out var carry) && type != ShaderInputType.CBuffer) {
                continue;
            }

            ushort slot, existingSize;
            if (existingResources.TryGetValue(resource.Name, out var existing)) {
                slot = existing.Slot;
                existingSize = existing.Size;
            } else {
                slot = type == ShaderInputType.CBuffer ? (ushort)65535 : (ushort)2;
                existingSize = 0;
            }

            resources[resource.Name] = new(
                resource.Name,
                resource.Type,
                slot,
                type == ShaderInputType.CBuffer ? Math.Max(carry?.Size ?? 0, resource.Size) : existingSize
            );
        }
    }

    public static ShaderPackage FromShaderPackageBytes(ReadOnlySpan<byte> bytes)
        => ShaderReaderWriter.ReadPackage(bytes);

    public byte[] ToShaderPackageBytes()
    {
        using var buffer = new MemoryStream();
        ShaderReaderWriter.Write(this, buffer);

        return buffer.ToArray();
    }

    public void WriteShaderPackageTo(Stream destination)
        => ShaderReaderWriter.Write(this, destination);

    void IBytesConvertible.WriteTo(Stream destination)
        => ShaderReaderWriter.Write(this, destination);
}
