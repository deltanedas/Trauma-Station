// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Barks;
using Content.Shared.Humanoid;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Humanoid;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Preferences;

/// <summary>
/// Trauma - extra profile data
/// </summary>
public sealed partial class HumanoidCharacterProfile
{
    private static readonly ProtoId<SpeciesPrototype> BeastSpecies = "Beast";

    [DataField]
    public ProtoId<BarkPrototype> BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;

    /// <summary>
    /// Changes to mastery level of every skill for this character, added to the species masteries.
    /// </summary>
    [DataField]
    public KnowledgeProfile Knowledge = new();

    /// <summary>
    /// The beastmen-specific profile data.
    /// </summary>
    [DataField]
    public BeastProfile? Beast;

    public static ProtoId<BarkPrototype> RandomBark(IRobustRandom random, IPrototypeManager proto, string species)
    {
        var barks = new List<ProtoId<BarkPrototype>>();
        foreach (var bark in proto.EnumeratePrototypes<BarkPrototype>())
        {
            if (bark.RoundStart && bark.SpeciesWhitelist?.Contains(species) != false)
                barks.Add(bark.ID);
        }

        return random.Pick(barks);
    }

    public HumanoidCharacterProfile WithBarkVoice(ProtoId<BarkPrototype> barkVoice)
        => new(this) { BarkVoice = barkVoice };

    public HumanoidCharacterProfile WithKnowledge(KnowledgeProfile knowledge)
        => new(this) { Knowledge = knowledge };

    public HumanoidCharacterProfile WithBeast(BeastProfile? beast)
        => new(this) { Beast = beast };

    private void EnsureValidTrauma(IDependencyCollection collection, IPrototypeManager proto)
    {
        if (!proto.HasIndex(BarkVoice))
            BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;

        var entMan = collection.Resolve<IEntityManager>();
        var knowledge = entMan.System<CommonKnowledgeSystem>();
        var parent = proto.Index(Species).Knowledge;
        knowledge.EnsureProfileValid(parent, ref Knowledge);

        if (Species == BeastSpecies)
        {
            Beast ??= new();
            Beast.EnsureValid(proto);
        }
        else
        {
            Beast = null;
        }
    }
}
