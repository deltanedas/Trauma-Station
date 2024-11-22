// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Trauma.Common.RadialSelector;
using Content.Trauma.Shared.BloodCult.Constructs.SoulShard;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.BloodCult.Constructs.Shell;

public sealed partial class ConstructShellSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConstructShellComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ConstructShellComponent, ComponentInit>(OnShellInit);
        SubscribeLocalEvent<ConstructShellComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ConstructShellComponent, RadialSelectorSelectedMessage>(OnConstructSelected);
        SubscribeLocalEvent<ConstructShellComponent, ComponentRemove>(OnShellRemove);
    }

    private void OnGetVerbs(Entity<ConstructShellComponent> shell, ref GetVerbsEvent<ExamineVerb> args)
    {
        var shellUid = shell.Owner;
        if (_ui.IsUiOpen(shellUid, RadialSelectorUiKey.Key))
            return;

        // Holy shitcode.
        Action action;
        if (args.User == shellUid)
        {
            action = () =>
            {
                _ui.SetUiState(shellUid, RadialSelectorUiKey.Key, new RadialSelectorState(shell.Comp.Constructs));
                _ui.TryToggleUi(shellUid, RadialSelectorUiKey.Key, shell);
            };
        }
        else if (_slots.GetItemOrNull(shell, shell.Comp.ShardSlotId) is { } shard && args.User == shard &&
             TryComp(shard, out SoulShardComponent? soulShard))
        {
            action = () =>
            {
                _ui.SetUiState(shellUid,
                    RadialSelectorUiKey.Key,
                    new RadialSelectorState(soulShard.IsBlessed ? shell.Comp.PurifiedConstructs : shell.Comp.Constructs));
                _ui.TryToggleUi(shellUid, RadialSelectorUiKey.Key, shard);
            };
        }
        else
            return;

        args.Verbs.Add(new ExamineVerb
        {
            DoContactInteraction = true,
            Text = Loc.GetString("soul-shard-selector-form"),
            Icon = new SpriteSpecifier.Rsi(
                new("/Textures/_Trauma/BloodCult/Entities/Items/construct_shell.rsi"), "icon"),
            Act = action
        });
    }

    private void OnShellInit(Entity<ConstructShellComponent> shell, ref ComponentInit args)
    {
        _slots.AddItemSlot(shell, shell.Comp.ShardSlotId, shell.Comp.ShardSlot);
    }

    private void OnInsertAttempt(Entity<ConstructShellComponent> shell, ref ContainerIsInsertingAttemptEvent args)
    {
        var item = args.EntityUid;
        var shellUid = shell.Owner;
        if (!TryComp(item, out SoulShardComponent? soulShard) ||
            _ui.IsUiOpen(shellUid, RadialSelectorUiKey.Key))
            return;

        if (!TryComp<MindContainerComponent>(item, out var mindContainer) || !mindContainer.HasMind)
        {
            _popup.PopupEntity(Loc.GetString("soul-shard-try-insert-no-soul"), shell);
            args.Cancel();
            return;
        }

        _slots.SetLock(shell, shell.Comp.ShardSlotId, true);
        _ui.SetUiState(shellUid,
            RadialSelectorUiKey.Key,
            new RadialSelectorState(soulShard.IsBlessed ? shell.Comp.PurifiedConstructs : shell.Comp.Constructs));

        _ui.TryToggleUi(shellUid, RadialSelectorUiKey.Key, item);
    }

    private void OnConstructSelected(Entity<ConstructShellComponent> shell, ref RadialSelectorSelectedMessage args)
    {
        if (!_mind.TryGetMind(args.Actor, out var mindId, out var mind))
            return;

        _ui.CloseUi(shell.Owner, RadialSelectorUiKey.Key);
        var coords = Transform(shell).Coordinates;
        var construct = PredictedSpawnAtPosition(args.SelectedItem, coords);
        _mind.TransferTo(mindId, construct, mind: mind);
        // TODO: unvisit or something??? set this as original entity??
        PredictedDel(shell.Owner);
    }

    private void OnShellRemove(Entity<ConstructShellComponent> shell, ref ComponentRemove args)
    {
        _slots.RemoveItemSlot(shell, shell.Comp.ShardSlot);
    }
}
