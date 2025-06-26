using Refulgence.IO;

namespace Refulgence.Dxbc.ResourceDefinition;

public sealed class ResourceBinding
{
    public string                      Name = string.Empty;
    public ShaderInputType             InputType;
    public ResourceReturnType          ReturnType;
    public ShaderResourceViewDimension ViewDimension;
    public uint                        NumSamples;
    public uint                        BindPoint;
    public uint                        BindCount;
    public ShaderInputFlags            InputFlags;

    internal const int SizeInStream = 32;

    internal static ResourceBinding Read(ref SpanBinaryReader reader)
    {
        var binding = new ResourceBinding();
        var nameOffset = reader.Read<uint>();
        binding.Name = reader.ReadString((int)nameOffset);
        binding.InputType = reader.Read<ShaderInputType>();
        binding.ReturnType = reader.Read<ResourceReturnType>();
        binding.ViewDimension = reader.Read<ShaderResourceViewDimension>();
        binding.NumSamples = reader.Read<uint>();
        binding.BindPoint = reader.Read<uint>();
        binding.BindCount = reader.Read<uint>();
        binding.InputFlags = reader.Read<ShaderInputFlags>();

        return binding;
    }

    internal void WriteTo(StringPool strings, SubStreamOrchestrator orchestrator)
    {
        var stream = strings.Data;
        orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L)
                    .PointeePosition = strings.FindOrAddString(Name, true).Offset;
        stream.Write(InputType);
        stream.Write(ReturnType);
        stream.Write(ViewDimension);
        stream.Write(NumSamples);
        stream.Write(BindPoint);
        stream.Write(BindCount);
        stream.Write(InputFlags);
    }
}
