namespace Refulgence.Xiv.ShaderPackages;

public sealed class ShaderKey(Name key, Name defaultValue)
{
    public readonly Name Key = key;

    public readonly HashSet<Name> Values = new(4)
    {
        defaultValue,
    };

    public Name DefaultValue = defaultValue;
}
