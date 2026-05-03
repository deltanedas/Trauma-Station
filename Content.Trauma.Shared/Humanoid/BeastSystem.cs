// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Trauma.Common.Humanoid;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Humanoid;

public sealed partial class BeastSystem : CommonBeastSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MutationSystem _mutation = default!;

    public const string MaxMutations = 16;

    /// <summary>
    /// Cache of every phenotype prototype's id.
    /// </summary>
    public List<ProtoId<BeastPhenotypePrototype>> AllPhenotypes = new();

    /// <summary>
    /// Human organs that always get added
    /// </summary>
    public static readonly EntProtoId[] InternalOrgans =
    [
        "OrganHumanBrain",
        "OrganHumanEyes",
        "OrganHumanTongue",
        "OrganHumanHeart",
        "OrganHumanLungs",
        "OrganHumanStomach",
        "OrganHumanLiver",
        "OrganHumanKidneys",
        "OrganHumanAppendix"
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPhenotypes();
    }

    public override void EnsureProfileValid(BeastProfile profile)
    {
        // remove anything that doesnt exist
        profile.Phenotypes.RemoveAll(id => !_proto.HasIndex<BeastPhenotypePrototype>(id));
        profile.Mutations.RemoveAll(id => !_mutation.BeastMutations.ContainsKey(id));

        // remove anything above the limits
        while (profile.Mutations.Count > MaxMutations)
        {
            profile.Mutations.RemoveAt(profile.Mutations.Count - 1);
        }
        foreach (var (organ, index) in profile.OrganIndices)
        {
            if (index >= profile.Phenotypes.Count)
                profile.OrganIndices[organ] = 0;
        }

        // add minimum required stuff to work
        if (profile.Phenotypes.Count == 0)
        {
            profile.Phenotypes.Add(_random.Pick(AllPhenotypes));
        }
        for (organ in _body.BodyParts)
        {
            if (!profile.OrganIndices.ContainsKey(organ))
                profile.OrganIndices[organ] = 0;
        }

        // make sure the cost is balanced
        var deficit = -GetProfilePoints(profile);
        while (deficit > 0)
        {
            // try add a random mutation of the highest possible costs first
            for (var target = deficit; target >= 1; target--)
            {
                if (!_mutation.BeastMutationsByPoints.TryGetValue(target, out var list))
                    continue;

                _picking.Clear();
                foreach (var id in list)
                {
                    // ignore any that are already present
                    if (!profile.Mutations.Contains(id))
                        _picking.Add(id);
                }

                if (_picking.Count == 0)
                    continue;

                var picked = _random.Pick(_picking);
                profile.Mutations.Add(_mutation.BeastMutations[picked]);
                break;
            }
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<BeastPhenotypePrototype>())
            LoadPhenotypes();
    }

    private void LoadPhenotypes()
    {
        AllPhenotypes.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<BeastPhenotypePrototype>())
        {
            AllPhenotypes.Add(proto.ID);
        }
    }

    public int GetProfilePoints(BeastProfile profile)
    {
        var points = 0;
        foreach (var id in profile.Phenotypes)
        {
            points -= _proto.Resolve(id, out var proto) ? proto.Cost : 0;
        }

        foreach (var id in profile.Mutations)
        {
            points += _mutation.BeastMutations.GetValueOrDefault(id);
        }
        return points;
    }
}
