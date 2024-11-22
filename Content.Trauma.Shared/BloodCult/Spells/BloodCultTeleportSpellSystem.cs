// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.ListViewSelector;
using Content.Shared.DoAfter;
using Content.Trauma.Shared.BloodCult.Runes;
using Content.Trauma.Shared.BloodCult.Runes.Teleport;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class BloodCultTeleportSpellSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private CultRuneSystem _rune = default!;
    [Dependency] private CultRuneTeleportSystem _runeTeleport = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultTeleportActionComponent, BloodCultTeleportEvent>(OnTeleport);
        SubscribeLocalEvent<CultTeleportActionComponent, ListViewItemSelectedMessage>(OnTeleportRuneSelected);
        SubscribeLocalEvent<CultTeleportActionComponent, TeleportActionDoAfterEvent>(OnTeleportDoAfter);
    }

    private void OnTeleport(Entity<CultTeleportActionComponent> ent, ref BloodCultTeleportEvent ev)
    {
        var user = ev.Performer;
        if (ev.Handled || !_runeTeleport.TryGetTeleportRunes(user, out var runes))
            return;

        var action = ev.Action; // action stores the UI
        _ui.SetUiState(action.Owner, ListViewSelectorUiKey.Key, new ListViewSelectorState(runes));
        _ui.TryToggleUi(action.Owner, ListViewSelectorUiKey.Key, user);
        ev.Handled = true;
    }

    private void OnTeleportRuneSelected(Entity<CultTeleportActionComponent> ent,
        ref ListViewItemSelectedMessage args)
    {
        if (!args.MetaData.TryGetValue("target", out var rawTarget) || rawTarget is not EntityUid target ||
            !args.MetaData.TryGetValue("duration", out var rawDuration) || rawDuration is not TimeSpan duration)
            return;

        // TODO: do you know what a fucking netentity is bruh
        var rune = EntityUid.Parse(args.SelectedItem.Id);
        if (TerminatingOrDeleted(rune) || !HasComp<CultRuneComponent>(rune))
            return;

        var user = args.Actor;
        var teleportDoAfter = new TeleportActionDoAfterEvent
        {
            Rune = GetNetEntity(rune)
        };
        var doAfterArgs = new DoAfterArgs(EntityManager, user, duration, teleportDoAfter, eventTarget: ent.Owner);

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnTeleportDoAfter(Entity<CultTeleportActionComponent> ent, ref TeleportActionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not {} target)
            return;

        var rune = GetEntity(args.Rune);
        var coords = Transform(rune).Coordinates;
        _teleport.Teleport(target, coords, ent.Comp.TeleportInSound, ent.Comp.TeleportOutSound, user: args.User);
    }
}
