// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Mutation component that lets beastmen pick it in the character editor.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MutationSystem))]
public sealed partial class BeastMutationComponent : Component
{
    /// <summary>
    /// How many points this mutation gives you for your character.
    /// Negative points are for actually good things.
    /// </summary>
    [DataField(required: true)]
    public int Points;
}
