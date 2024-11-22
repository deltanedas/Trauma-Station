// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.BloodCult;
using Content.Trauma.Shared.BloodCult.Constructs;
using Content.Trauma.Shared.BloodCult.Spells;
using Content.Trauma.Shared.BloodCult.UI;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.BloodCult.BloodRites;

public sealed partial class BloodRitesSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodRitesAuraComponent, ExaminedEvent>(OnExamining);

        SubscribeLocalEvent<BloodRitesAuraComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BloodRitesAuraComponent, BloodRitesExtractDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<BloodRitesAuraComponent, MeleeHitEvent>(OnCultistHit);

        SubscribeLocalEvent<BloodRitesAuraComponent, BloodRitesMessage>(OnRitesMessage);
    }

    private void OnExamining(Entity<BloodRitesAuraComponent> rites, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("blood-rites-stored-blood", ("amount", rites.Comp.StoredBlood.ToString())));
    }

    private void OnAfterInteract(Entity<BloodRitesAuraComponent> rites, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            args.Target is not {} target ||
            target == user ||
            rites.Comp.Extracting ||
            _cult.IsCultist(target)) // no stabbing your fellow cultists
            return;

        if (!HasComp<BloodstreamComponent>(target))
            return;

        var ev = new BloodRitesExtractDoAfterEvent();
        var time = rites.Comp.BloodExtractionTime;
        var doAfterArgs = new DoAfterArgs(EntityManager, user, time, ev, eventTarget: rites, target: target, used: rites)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        };

        rites.Comp.Extracting = _doAfter.TryStartDoAfter(doAfterArgs);
        Dirty(rites);

        args.Handled = true;
    }

    private void OnDoAfter(Entity<BloodRitesAuraComponent> rites, ref BloodRitesExtractDoAfterEvent args)
    {
        rites.Comp.Extracting = false;
        Dirty(rites);

        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            _bloodstream.DrainBlood(target) is not {} blood)
            return;

        rites.Comp.StoredBlood += blood.Volume;
        _audio.PlayPredicted(rites.Comp.BloodRitesAudio, rites, args.User);
        args.Handled = true;
    }

    private void OnCultistHit(Entity<BloodRitesAuraComponent> rites, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        var playSound = false;
        var user = args.User;
        foreach (var target in args.HitEntities)
        {
            if (!_cult.IsCultist(target))
                return;

            if (TryComp(target, out BloodstreamComponent? bloodstream))
            {
                playSound |= RestoreBloodLevel(rites, user, (target, bloodstream));
            }

            if (TryComp(target, out DamageableComponent? damageable))
            {
                playSound |= Heal(rites, user, (target, damageable));
            }
        }

        if (playSound)
            _audio.PlayPredicted(rites.Comp.BloodRitesAudio, rites, user);
    }

    private void OnRitesMessage(Entity<BloodRitesAuraComponent> rites, ref BloodRitesMessage args)
    {
        var id = args.SelectedProto;
        if (!rites.Comp.Crafts.TryGetValue(id, out var cost) || rites.Comp.StoredBlood < cost)
            return; // malf client

        rites.Comp.StoredBlood -= cost; // incase multiple messages in the same tick ?!
        PredictedDel(rites.Owner);

        var user = args.Actor;
        var item = PredictedSpawnNextToOrDrop(args.SelectedProto, user);
        _hands.TryPickup(user, item);
    }

    private bool Heal(Entity<BloodRitesAuraComponent> rites, EntityUid user, Entity<DamageableComponent?> target)
    {
        var damage = _damageable.GetAllDamage(target);
        if (damage.GetTotal() == 0)
            return false;

        if (_mob.IsDead(target))
        {
            _popup.PopupClient(Loc.GetString("blood-rites-heal-dead"), target, user);
            return false;
        }

        if (!HasComp<BloodstreamComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("blood-rites-heal-no-bloodstream"), target, user);
            return false;
        }

        var bloodCost = rites.Comp.HealingCost;
        if (target.Owner == user)
            bloodCost *= rites.Comp.SelfHealRatio;

        if (bloodCost >= rites.Comp.StoredBlood)
        {
            _popup.PopupClient(Loc.GetString("blood-rites-not-enough-blood"), rites, user);
            return false;
        }

        var healingLeft = rites.Comp.TotalHealing;

        foreach (var (type, value) in damage.DamageDict)
        {
            if (!_proto.Resolve(type, out var damageType))
                continue;

            var toHeal = value;
            if (toHeal > healingLeft)
                toHeal = healingLeft;

            _damageable.ChangeDamage(target, new DamageSpecifier(damageType, -toHeal));

            healingLeft -= toHeal;
            if (healingLeft == 0)
                break;
        }

        rites.Comp.StoredBlood -= bloodCost;
        Dirty(rites);
        return true;
    }

    private bool RestoreBloodLevel(
        Entity<BloodRitesAuraComponent> rites,
        EntityUid user,
        Entity<BloodstreamComponent> target
    )
    {
        if (target.Comp.BloodSolution is null)
            return false;

        _bloodstream.FlushChemicals(target, 10);
        // FIXME: need to update this
        var missingBlood = target.Comp.BloodSolution.Value.Comp.Solution.AvailableVolume;
        if (missingBlood == 0)
            return false;

        var bloodCost = missingBlood * rites.Comp.BloodRegenerationRatio;
        if (target.Owner == user)
            bloodCost *= rites.Comp.SelfHealRatio;

        if (bloodCost > rites.Comp.StoredBlood)
        {
            _popup.PopupEntity("blood-rites-no-blood-left", rites, user);
            bloodCost = rites.Comp.StoredBlood;
        }

        _bloodstream.TryModifyBleedAmount(target, -3);
        _bloodstream.TryModifyBloodLevel(target, bloodCost / rites.Comp.BloodRegenerationRatio);

        rites.Comp.StoredBlood -= bloodCost;
        return true;
    }
}
