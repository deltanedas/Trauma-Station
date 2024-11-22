// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Blocking;

namespace Content.Trauma.Shared.BloodCult.Items;

public sealed partial class CultItemSystem : EntitySystem
{
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultItemComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CultItemComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<CultItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<CultItemComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<CultItemComponent, BlockAttemptEvent>(OnBlockAttempt);
    }

    private void OnActivate(Entity<CultItemComponent> item, ref ActivateInWorldEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Handled = true;
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-generic"));
    }

    private void OnBeforeThrow(Entity<CultItemComponent> item, ref BeforeThrowEvent args)
    {
        if (CanUse(args.PlayerUid))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.PlayerUid, Loc.GetString("cult-item-component-throw-fail"));
    }

    private void OnEquipAttempt(Entity<CultItemComponent> item, ref BeingEquippedAttemptEvent args)
    {
        if (CanUse(args.EquipTarget))
            return;

        args.Cancel();
        KnockdownAndDropItem(item, args.EquipTarget, Loc.GetString("cult-item-component-equip-fail"));
    }

    private void OnMeleeAttempt(Entity<CultItemComponent> item, ref AttemptMeleeEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-attack-fail"));
    }

    private void OnBlockAttempt(Entity<CultItemComponent> item, ref BlockAttemptEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-block-fail"));
    }

    private void KnockdownAndDropItem(Entity<CultItemComponent> item, EntityUid user, string message)
    {
        _popup.PopupPredicted(message, item, user);
        _stun.TryKnockdown(user, item.Comp.KnockdownDuration, true);
        _hands.TryDrop(user);
    }

    private bool CanUse(EntityUid uid)
        => _ghostQuery.HasComp(uid) || _cult.IsCultist(uid);
}
