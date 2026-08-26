// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using System.Text;

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
    /// The human-readable name of this phenotype.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// The RSI to use for bodyparts.
    /// </summary>
    [DataField(required: true)]
    public string PartsRsi = string.Empty;

    /// <summary>
    /// The RSI to use for internal organs.
    /// </summary>
    [DataField]
    public string OrgansRsi = "Mobs/Species/Human/organs.rsi";

    /// <summary>
    /// Optional displacement maps RSI
    /// </summary>
    [DataField]
    public string? DisplacementsRsi;

    /// <summary>
    /// How many points having this phenotype active costs.
    /// You will have to take negative mutations to balance your points.
    /// Negative cost will instead give you points to spend
    /// </summary>
    [DataField]
    public int Cost;

    /// <summary>
    /// Premium species :)
    /// </summary>
    [DataField]
    public bool PatronOnly;

    /// <summary>
    /// Indefinite article for this phenotype: a vulp, an avali, etc.
    /// </summary>
    [DataField]
    public string IndefiniteArticle = "a";

    public void AddIndefinite(StringBuilder sb)
    {
        sb.Append(IndefiniteArticle);
        sb.Append(' ');
        sb.Append(Name);
    }
}
