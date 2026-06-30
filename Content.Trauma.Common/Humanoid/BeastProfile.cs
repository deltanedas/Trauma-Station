// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using System.Text.Json.Serialization;

namespace Content.Trauma.Common.Humanoid;

/// <summary>
/// Character profile data specific to beastmen.
/// Fields using just string are not guaranteed to be valid and must be checked.
/// Organ categories are not expected to ever be removed.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public sealed partial class BeastProfile
{
    /// <summary>
    /// Every phenotype organs can be picked from.
    /// If this has no valid phenotypes, one must be randomly picked when loading.
    /// </summary>
    [JsonPropertyName("phenotypes")]
    public List<string> Phenotypes;

    /// <summary>
    /// Every organ slot and the phenotype index to take the organ from.
    /// </summary>
    [JsonPropertyName("organIndices")]
    public Dictionary<ProtoId<OrganCategoryPrototype>, int> OrganIndices;

    /// <summary>
    /// All mutations to add when spawning.
    /// </summary>
    [JsonPropertyName("mutations")]
    public List<string> Mutations;

    public BeastProfile()
    {
        Phenotypes = new();
        OrganIndices = new();
        Mutations = new();
    }

    public BeastProfile(BeastProfile other)
    {
        Phenotypes = new(other.Phenotypes);
        OrganIndices = new(other.OrganIndices);
        Mutations = new(other.Mutations);
    }
}
