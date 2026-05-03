using Content.Shared.Body;

namespace Content.Trauma.Common.Humanoid;

/// <summary>
/// Character profile data specific to beastmen.
/// Fields using just string are not guaranteed to be valid and must be checked.
/// Organ categories are not expected to ever be removed.
/// </summary>
[DataRecord]
public sealed class BeastProfile
{
    /// <summary>
    /// Every phenotype organs can be picked from.
    /// If this has no valid phenotypes, one must be randomly picked when loading.
    /// </summary>
    public List<string> Phenotypes = new();

    /// <summary>
    /// Every organ slot and the phenotype index to take the organ from.
    /// </summary>
    public Dictionary<ProtoId<OrganCategoryPrototype>, int> OrganIndices = new();

    /// <summary>
    /// All mutations to add when spawning.
    /// </summary>
    public List<string> Mutations = new();
}
