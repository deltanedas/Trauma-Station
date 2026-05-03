using Content.Shared.Body;

namespace Content.Trauma.Shared.Humanoid;

/// <summary>
/// Phenotype that specifies the base sprites for your bodyparts.
/// You can pick parts from any active phenotype in character creation.
/// </summary>
[Prototype]
public sealed partial class BeastPhenotypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The bodyparts to spawn for each organ.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntProtoId> Organs = default!;

    /// <summary>
    /// How many points having this phenotype active costs.
    /// You will have to take negative mutations to balance your points.
    /// Negative cost will instead give you points to spend
    /// </summary>
    [DataField]
    public int Cost;
}
