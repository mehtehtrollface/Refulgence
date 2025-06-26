using Refulgence.Collections;

namespace Refulgence.Xiv.ShaderPackages;

public sealed class RenderNode(uint primarySelector)
{
    public readonly Dictionary<Name, Name>        MaterialValues = new(4);
    public readonly IndexedList<Name, RenderPass> Passes         = new(16, pass => pass.Name);

    public readonly byte[] PassIndices =
    [
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    ];

    public readonly Dictionary<Name, Name> SceneValues     = new(4);
    public readonly Dictionary<Name, Name> SystemValues    = new(4);
    public          uint                   PrimarySelector = primarySelector;
    public          Name                   SubViewValue0   = Name.Empty;
    public          Name                   SubViewValue1   = Name.Empty;
}
