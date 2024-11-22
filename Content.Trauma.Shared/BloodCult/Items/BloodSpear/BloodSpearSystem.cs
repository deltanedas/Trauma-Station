// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Trauma.Shared.BloodCult.Spells;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.BloodCult.Items.BloodSpear;

public sealed partial class BloodSpearSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodSpearComponent, EmbedEvent>(OnEmbed);

        SubscribeLocalEvent<BloodSpearComponent, GotEquippedHandEvent>(OnPickedUp);
        SubscribeLocalEvent<BloodSpearComponent, BloodSpearRecalledEvent>(OnSpearRecalled);

        SubscribeLocalEvent<BloodSpearComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentShutdown(Entity<BloodSpearComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.RecallAction);
    }

    private void OnEmbed(Entity<BloodSpearComponent> ent, ref EmbedEvent args)
    {
        if (!HasComp<HumanoidProfileComponent>(args.Embedded))
            return;

        _stun.TryUpdateParalyzeDuration(args.Embedded, ent.Comp.ParalyzeTime);
        PredictedQueueDel(ent);
    }

    private void OnPickedUp(Entity<BloodSpearComponent> ent, ref GotEquippedHandEvent args)
    {
        var user = args.User;
        // only cultists get to recall it
        if (ent.Comp.HasMaster || !_cult.IsCultist(user))
            return;

        var action = _actions.AddAction(user, ent.Comp.RecallActionId, container: ent.Owner);
        ent.Comp.RecallAction = action;

        ent.Comp.Master = user;
        ent.Comp.HasMaster = true; // only tell other clients that it has a master
        Dirty(ent);
    }

    private void OnSpearRecalled(Entity<BloodSpearComponent> ent, ref BloodSpearRecalledEvent args)
    {
        if (args.Handled || ent.Comp.Master is not {} master)
            return;

        args.Handled = true;

        if (_cult.IsCultist(master))
        {
            _hands.TryForcePickupAnyHand(master, ent.Owner);
            _audio.PlayPredicted(ent.Comp.RecallAudio, ent.Owner, args.Performer);
            return;
        }

        // no more spear for you chuddy
        ent.Comp.Master = null;
        ent.Comp.HasMaster = false;
        Dirty(ent);
    }
}
