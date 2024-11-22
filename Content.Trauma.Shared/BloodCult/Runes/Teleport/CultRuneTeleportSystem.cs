// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.ListViewSelector;
using Content.Trauma.Shared.BloodCult.UI;
using Content.Trauma.Shared.Teleportation;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.BloodCult.Runes.Teleport;

public sealed partial class CultRuneTeleportSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneTeleportComponent, RunePlacedEvent>(OnRunePlaced);
        SubscribeLocalEvent<CultRuneTeleportComponent, NameSelectedMessage>(OnNameSelected);
        SubscribeLocalEvent<CultRuneTeleportComponent, RuneInvokeEvent>(OnTeleportRuneInvoked);
        SubscribeLocalEvent<CultRuneTeleportComponent, ListViewItemSelectedMessage>(OnTeleportRuneSelected);
    }

    private void OnRunePlaced(Entity<CultRuneTeleportComponent> rune, ref RunePlacedEvent args)
    {
        _ui.OpenUi(rune.Owner, NameSelectorUiKey.Key, args.User);
    }

    private void OnNameSelected(Entity<CultRuneTeleportComponent> rune, ref NameSelectedMessage args)
    {
        rune.Comp.Name = args.Name;
    }

    private void OnTeleportRuneInvoked(Entity<CultRuneTeleportComponent> rune, ref RuneInvokeEvent args)
    {
        var runeUid = rune.Owner;
        if (_ui.IsUiOpen(runeUid, ListViewSelectorUiKey.Key))
            return;

        if (!TryGetTeleportRunes(runeUid, out var runes, args.User))
        {
            args.Popup = Loc.GetString("cult-teleport-not-found");
            return;
        }

        _ui.SetUiState(runeUid, ListViewSelectorUiKey.Key, new ListViewSelectorState(runes));
        _ui.TryToggleUi(runeUid, ListViewSelectorUiKey.Key, args.User);
        args.Handled = true;
    }

    private void OnTeleportRuneSelected(Entity<CultRuneTeleportComponent> origin, ref ListViewItemSelectedMessage args)
    {
        var user = args.Actor;
        if (!NetEntity.TryParse(args.SelectedItem.Id, out var netDest))
            return;

        var dest = GetEntity(netDest);
        if (!HasComp<CultRuneTeleportComponent>(dest))
            return;

        var targets = _cult.GetTargetsNearRune(origin, origin.Comp.TeleportGatherRange);
        var coords = Transform(dest).Coordinates;

        foreach (var target in targets)
        {
            // sounds played separately to avoid spam with multiple targets
            _teleport.Teleport(target, coords, user: user);
        }

        _audio.PlayPredicted(origin.Comp.TeleportOutSound, origin, user);
        _audio.PlayPredicted(origin.Comp.TeleportInSound, coords, user);
    }

    public bool TryGetTeleportRunes(EntityUid user, out List<ListViewSelectorEntry> runes, EntityUid? exclude = null)
    {
        var runeQuery = EntityQueryEnumerator<CultRuneTeleportComponent>();
        runes = new List<ListViewSelectorEntry>();
        while (runeQuery.MoveNext(out var targetRune, out var teleportRune))
        {
            if (targetRune == exclude)
                continue;

            var entry = new ListViewSelectorEntry(GetNetEntity(targetRune).ToString(), teleportRune.Name);
            runes.Add(entry);
        }

        return runes.Count != 0;
    }
}
