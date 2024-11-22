// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.ListViewSelector;
using Content.Shared.Cuffs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.Teleportation;

namespace Content.Trauma.Shared.BloodCult.Runes.Summon;

public sealed partial class CultRuneSummonSystem : EntitySystem
{
    [Dependency] private CultRuneSystem _rune = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private TeleportSystem _teleport = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneSummonComponent, RuneInvokeEvent>(OnSummonRuneInvoked);
        SubscribeLocalEvent<CultRuneSummonComponent, ListViewItemSelectedMessage>(OnCultistSelected);
    }

    private void OnSummonRuneInvoked(Entity<CultRuneSummonComponent> rune, ref RuneInvokeEvent args)
    {
        var runeUid = rune.Owner;
        if (_ui.IsUiOpen(runeUid, ListViewSelectorUiKey.Key))
            return;

        if (_net.IsClient)
            return; // client can't predict cultists outside of pvs range sorry. also won't predict the damage because that's jank

        var cultistsQuery = EntityQueryEnumerator<BloodCultistComponent>();
        var cultist = new List<ListViewSelectorEntry>();
        while (cultistsQuery.MoveNext(out var cultistUid, out _))
        {
            if (args.Invokers.Contains(cultistUid))
                continue;

            var metaData = MetaData(cultistUid);
            var entry = new ListViewSelectorEntry(GetNetEntity(cultistUid).ToString(),
                metaData.EntityName, // doxxed
                metaData.EntityDescription);

            cultist.Add(entry);
        }

        var user = args.User;
        if (cultist.Count == 0)
        {
            args.Popup = Loc.GetString("cult-rune-no-targets");
            return;
        }

        _ui.SetUiState(runeUid, ListViewSelectorUiKey.Key, new ListViewSelectorState(cultist));
        _ui.TryToggleUi(runeUid, ListViewSelectorUiKey.Key, user);
        args.Handled = true;
    }

    private void OnCultistSelected(Entity<CultRuneSummonComponent> ent, ref ListViewItemSelectedMessage args)
    {
        if (!NetEntity.TryParse(args.SelectedItem.Id, out var netTarget))
            return;

        var target = GetEntity(netTarget);
        if (!Exists(target))
            return; // client won't predict teleporting cultists outside of PVS range

        var user = args.Actor;
        if (!_cult.IsCultist(target))
        {
            Log.Error($"Evil client {ToPrettyString(user)} tried to summon non=cultist {ToPrettyString(target)}!");
            return;
        }

        // client will predict never being cuffed due to PVS but it's very unlikely to worry about
        if (TryComp(target, out CuffableComponent? cuffable) && cuffable.CuffedHandCount > 0)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-summon-cuffed"), ent, user);
            return;
        }

        var pos = Transform(ent).Coordinates;
        _teleport.Teleport(target, pos, ent.Comp.TeleportSound, user: user);
    }
}
