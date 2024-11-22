using Content.Shared.Whitelist;

namespace Content.Server.Stunnable.Components;

public sealed partial class StunOnCollideComponent
{
    /// <summary>
    /// Blacklist for entities it will not try to stun.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
