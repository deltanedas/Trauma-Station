// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Spells;

/// <summary>
/// Mind component that holds spells for cultists.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BloodCultSpellsComponent : Component
{
    [DataField]
    public TimeSpan SpellCreationTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public List<EntityUid> SelectedSpells = new();

    [DataField, AutoNetworkedField]
    public int MaxSpells = 1;

    /// <summary>
    /// True while the spell creation doafter is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Creating;

    /// <summary>
    /// Actions that you can create.
    /// </summary>
    [DataField]
    public List<EntProtoId> AvailableActions = new()
    {
        "ActionBloodCultStun",
        "ActionBloodCultTeleport",
        "ActionBloodCultEmp",
        "ActionBloodCultShadowShackles",
        "ActionBloodCultTwistedConstruction",
        "ActionBloodCultSummonCombatEquipment",
        "ActionBloodCultSummonRitualDagger",
        "ActionBloodCultBloodRites"
    };

    /// <summary>
    /// Since radial selector menu doesn't have metadata, we use this to toggle between remove and
    /// add spells modes.
    /// </summary>
    [DataField]
    public bool AddSpellsMode = true;
}
