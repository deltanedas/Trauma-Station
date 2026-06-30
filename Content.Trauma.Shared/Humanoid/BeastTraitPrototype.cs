using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Humanoid;

/// <summary>
/// A trait beastmen can take, some are required to balance the phenotype's <c>Cost</c>.
/// </summary>
[Prototype]
public sealed partial class BeastTraitPrototype : IPrototype
{
    /// <summary>
    /// Human readable name for this mutation.
    /// </summary>
    [DataField(required: true)]
    public string Name;

    /// <summary>
    /// How many points this mutation gives you for your character.
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
    /// Add an unremovable mutation to the mob when picked.
    /// </summary>
    [DataField]
    public EntProtoId<MutationComponent>? Mutation;
}
