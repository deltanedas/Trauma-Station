// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Trauma.Common.Humanoid;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Random;
using System.Linq;
using System.Text;

namespace Content.Trauma.Shared.Humanoid;

public sealed partial class BeastSystem : CommonBeastSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private EntityQuery<MutatableComponent> _mutatableQuery = default!;

    /// <summary>
    /// Cache of every phenotype prototype's id.
    /// </summary>
    public List<ProtoId<BeastPhenotypePrototype>> AllPhenotypes = new();
    public Dictionary<int, List<BeastTraitPrototype>> TraitsByPoints = new();

    private List<ProtoId<BeastTraitPrototype>> _picking = new();

    public override void Initialize()
    {
        base.Initialize();

        LoadPhenotypes();
        LoadTraits();
    }

    public override void EnsureProfileValid(BeastProfile profile)
    {
        // remove anything that doesnt exist
        profile.Phenotypes.RemoveAll(id => !ProtoMan.HasIndex<BeastPhenotypePrototype>(id));
        profile.Traits.RemoveAll(id => !ProtoMan.HasIndex<BeastTraitPrototype>(id));

        // remove anything above the limits
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
        foreach (var organ in BodySystem.BodyParts)
        {
            if (!profile.OrganIndices.ContainsKey(organ))
                profile.OrganIndices[organ] = 0;
        }

        // make sure the cost is balanced
        var deficit = -GetProfilePoints(profile);
        while (deficit > 0)
        {
            // try add a random trait of the highest possible costs first
            var found = false;
            for (var target = deficit; target >= 1; target--)
            {
                if (!TraitsByPoints.TryGetValue(target, out var list))
                    continue;

                _picking.Clear();
                foreach (var trait in list)
                {
                    if (CanAddTrait(profile, trait))
                        _picking.Add(trait.ID);
                }

                if (_picking.Count == 0)
                    continue;

                deficit -= target;

                var picked = _random.Pick(_picking);
                profile.Traits.Add(picked);
                found = true;
                break;
            }

            if (!found)
            {
                var picked = string.Join(", ", profile.Traits);
                Log.Error($"Couldn't find enough mutations to balance point deficit of {deficit}! Currently have these traits: {picked}");
                break;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnProfileChanged(Entity<HumanoidProfileComponent> ent, ref BeastProfileChangedEvent args)
    {
        if (args.Profile is not { } beast)
            return;

        // TODO: remove old mutations?
        var mutatable = _mutatableQuery.CompOrNull(ent);

        var target = new Entity<MutatableComponent?>(ent, mutatable);
        foreach (var id in beast.Traits)
        {
            if (!ProtoMan.Resolve<BeastTraitPrototype>(id, out var trait))
                continue;

            if (trait.Mutation is not { } mutation || mutatable == null)
                continue;

            if (!_mutation.AddMutation(target, mutation, automatic: true))
            {
                Log.Error($"Failed to add mutation {id} to {ToPrettyString(ent)}!");
                continue;
            }

            // no removing all your downsides 2m in
            EnsureComp<UnremoveableComponent>(mutatable.Mutations[id]);
        }
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<BeastPhenotypePrototype>())
            LoadPhenotypes();
        if (args.WasModified<BeastTraitPrototype>())
            LoadTraits();
    }

    private void LoadPhenotypes()
    {
        AllPhenotypes.Clear();
        foreach (var proto in ProtoMan.EnumeratePrototypes<BeastPhenotypePrototype>())
        {
            AllPhenotypes.Add(proto.ID);
        }
    }

    private void LoadTraits()
    {
        foreach (var list in TraitsByPoints.Values)
        {
            list.Clear();
        }

        foreach (var trait in ProtoMan.EnumeratePrototypes<BeastTraitPrototype>())
        {
            var list = TraitsByPoints.GetOrNew(trait.Points);
            list.Add(trait);
        }
    }

    public int GetProfilePoints(BeastProfile profile)
    {
        var points = 0;
        foreach (var id in profile.Phenotypes)
        {
            if (ProtoMan.Resolve<BeastPhenotypePrototype>(id, out var proto))
                points -= proto.Cost;
        }

        foreach (var id in profile.Traits)
        {
            if (ProtoMan.Resolve<BeastTraitPrototype>(id, out var trait))
                points += trait.Points;
        }
        return points;
    }

    /// <summary>
    /// Returns true if a trait can be added to a <see cref="BeastProfile"/>.
    /// </summary>
    public bool CanAddTrait(BeastProfile profile, BeastTraitPrototype trait)
        => CanAddTrait(profile, trait, out _);

    /// <summary>
    /// Returns true if a trait can be added to a <see cref="BeastProfile"/>, giving a reason if it can't.
    /// </summary>
    public bool CanAddTrait(BeastProfile profile, BeastTraitPrototype trait, out string? reason)
    {
        reason = null;
        if (trait.Whitelist is { } whitelist && !whitelist.Any(id => profile.Phenotypes.Contains(id)))
        {
            reason = $"You must be {FormatPhenotypes(whitelist)}";
            return false;
        }

        if (trait.Blacklist is { } blacklist && blacklist.Any(id => profile.Phenotypes.Contains(id)))
        {
            reason = $"You cannot be {FormatPhenotypes(blacklist)}";
            return false;
        }

        // no conflicting traits present
        foreach (var conflict in trait.Conflicts)
        {
            if (profile.Traits.Contains(conflict))
                return false;
        }

        // no duplicates...
        return !profile.Traits.Contains(trait.ID);
    }

    // nicest sounding list of names for the context of a whitelist
    // 1: an apple
    // 2: an apple or banana
    // 3+: an apple, banana or an orange
    private string FormatPhenotypes(List<ProtoId<BeastPhenotypePrototype>> ids)
    {
        if (ids.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        var first = ProtoMan.Index(ids[0]);
        first.AddIndefinite(sb);
        if (ids.Count == 1)
            return sb.ToString();

        if (ids.Count == 2)
        {
            sb.Append(" or ");
            sb.Append(ProtoMan.Index(ids[1]).Name);
            return sb.ToString();
        }

        for (var i = 1; i < ids.Count - 1; i++)
        {
            sb.Append(", ");
            sb.Append(ProtoMan.Index(ids[i]).Name);
        }

        var last = ProtoMan.Index(ids[ids.Count - 1]);
        sb.Append(" or ");
        last.AddIndefinite(sb);
        return sb.ToString();
    }
}
