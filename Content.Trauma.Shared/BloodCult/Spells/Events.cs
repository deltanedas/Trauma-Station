// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class BloodCultTeleportEvent : EntityTargetActionEvent;

public sealed partial class BloodCultShacklesEvent : EntityTargetActionEvent
{
    [DataField]
    public EntProtoId ShacklesProto = "ShadowShackles";

    [DataField]
    public TimeSpan MuteDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(1);
}

public sealed partial class BloodCultTwistedConstructionEvent : EntityTargetActionEvent;

public sealed partial class SummonEquipmentEvent : InstantActionEvent
{
    /// <summary>
    /// Inventory slot to EntProtoId
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Prototypes = new();
}

public sealed partial class BloodSpearRecalledEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class TwistedConstructionDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CreateSpellDoAfterEvent : DoAfterEvent
{
    public EntProtoId ActionProtoId;

    public CreateSpellDoAfterEvent(EntProtoId id)
    {
        ActionProtoId = id;
    }

    public override DoAfterEvent Clone()
        => new CreateSpellDoAfterEvent(ActionProtoId);
}

[Serializable, NetSerializable]
public sealed partial class TeleportActionDoAfterEvent : DoAfterEvent
{
    // TODO: implement Clone properly i cant be fucked
    public NetEntity Rune;
    public SoundPathSpecifier TeleportInSound = new("/Audio/_Trauma/BloodCult/veilin.ogg");
    public SoundPathSpecifier TeleportOutSound = new("/Audio/_Trauma/BloodCult/veilout.ogg");

    public override DoAfterEvent Clone()
        => new TeleportActionDoAfterEvent()
        {
            Rune = this.Rune,
            TeleportInSound = this.TeleportInSound,
            TeleportOutSound = this.TeleportOutSound
        };
}

[Serializable, NetSerializable]
public sealed partial class BloodRitesExtractDoAfterEvent : SimpleDoAfterEvent;
