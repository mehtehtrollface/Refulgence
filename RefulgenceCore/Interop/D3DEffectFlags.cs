namespace Refulgence.Interop;

[Flags]
public enum D3DEffectFlags : uint
{
    /// <summary>
    ///     Compile the effects (.fx) file to a child effect. Child effects have no initializers for any shared values because
    ///     these child effects are initialized in the master effect (the effect pool).
    /// </summary>
    ChildEffect = 0x1,

    /// <summary>
    ///     Disables performance mode and allows for mutable state objects.
    ///     <para />
    ///     By default, performance mode is enabled. Performance mode disallows mutable state objects by preventing non-literal
    ///     expressions from appearing in state object definitions.
    /// </summary>
    AllowSlowOps = 0x2,
}
