// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Humanoid;

/// <summary>
/// A trait beastmen can take, some are required to balance the phenotype's <c>Cost</c>.
/// </summary>
[Prototype]
public sealed partial class BeastTraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The category this trait belongs to in the UI.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BeastTraitCategoryPrototype> Category;

    /// <summary>
    /// Human readable name for this trait.
    /// </summary>
    [DataField(required: true)]
    public string Name;

    /// <summary>
    /// Human readable description for this trait, shown when hovered.
    /// </summary>
    [DataField(required: true)]
    public string Desc;

    /// <summary>
    /// How many points this trait gives you for your character.
    /// Negative points are for actually good things.
    /// </summary>
    [DataField(required: true)]
    public int Points;

    /// <summary>
    /// Whitelist for phenotypes that can take this trait.
    /// </summary>
    [DataField]
    public List<ProtoId<BeastPhenotypePrototype>>? Whitelist;

    /// <summary>
    /// Blacklist for phenotypes that cannot take this trait.
    /// </summary>
    [DataField]
    public List<ProtoId<BeastPhenotypePrototype>>? Blacklist;

    /// <summary>
    /// Prevents other traits being taken if this one is taken.
    /// </summary>
    [DataField]
    public List<ProtoId<BeastTraitPrototype>> Conflicts = new();

    /// <summary>
    /// Add an unremovable mutation to the mob when picked.
    /// </summary>
    [DataField]
    public EntProtoId<MutationComponent>? Mutation;
}
