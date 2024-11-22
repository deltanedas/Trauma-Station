// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Shared.Cuffs;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Stunnable;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffect;
using Content.Trauma.Shared.BloodCult.Runes.Revive;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Runes.Offering;

public sealed partial class CultRuneOfferingSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedCultRuneReviveSystem _runeRevive = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public static readonly EntProtoId SoulShard = "SoulShard";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneOfferingComponent, RuneInvokeEvent>(OnOfferingRuneInvoked);
    }

    private void OnOfferingRuneInvoked(Entity<CultRuneOfferingComponent> ent, ref RuneInvokeEvent args)
    {
        var targets = _cult.GetTargetsNearRune(ent, ent.Comp.OfferingRange);
        targets.RemoveWhere(uid => _cult.IsCultist(uid));

        if (targets.Count == 0)
        {
            // TODO: popup
            return;
        }

        var target = targets.First();
        var user = args.User;
        // if the target is dead we should always sacrifice it.
        if (_mob.IsDead(target))
        {
            Sacrifice(target, user);
            return;
        }

        if (_mind.GetMind(target) == null ||
            _cult.IsTarget(user, target) ||
            HasComp<BibleUserComponent>(target) ||
            HasComp<MindShieldComponent>(target))
        {
            args.Handled = TrySacrifice(target, ent, user, args.Invokers.Count);
            return;
        }

        args.Handled = TryConvert(target, ent, user, args.Invokers.Count);
    }

    private bool TrySacrifice(EntityUid target,
        Entity<CultRuneOfferingComponent> rune,
        EntityUid user,
        int invokersAmount)
    {
        if (invokersAmount < rune.Comp.AliveSacrificeInvokersAmount)
            return false;

        _runeRevive.AddCharges(rune, rune.Comp.ReviveChargesPerOffering);
        Sacrifice(target, user);
        return true;
    }

    private void Sacrifice(EntityUid target, EntityUid user)
    {
        var pos = Transform(target).Coordinates;
        var shard = PredictedSpawnAtPosition(SoulShard, pos);
        _gibbing.Gib(target, user: user);

        var ev = new BloodCultSacrificedEvent(target, user);
        RaiseLocalEvent(ref ev);

        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return;

        _mind.TransferTo(mindId, shard, mind: mind);
        _mind.UnVisit(mindId);
    }

    private bool TryConvert(EntityUid target,
        Entity<CultRuneOfferingComponent> rune,
        EntityUid user,
        int invokersAmount)
    {
        if (invokersAmount < rune.Comp.ConvertInvokersAmount)
            return false;

        _runeRevive.AddCharges(rune, rune.Comp.ReviveChargesPerOffering);
        Convert(rune, target, user);
        return true;
    }

    private void Convert(Entity<CultRuneOfferingComponent> rune, EntityUid target, EntityUid user)
    {
        _cult.Convert(user, target);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(2f));
        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(2f));

        _cuffable.TryUncuff(target, user);

        _statusEffects.TryRemoveStatusEffect(target, "Muted");
        _damageable.ChangeDamage(target, rune.Comp.ConvertHealing, ignoreResistances: true);
    }
}

/// <summary>
/// Broadcast when a cultist sacrafices a mob.
/// </summary>
[ByRefEvent]
public record struct BloodCultSacrificedEvent(EntityUid Target, EntityUid User);
