// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Cuffs;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Trauma.Common.RadialSelector;
using Content.Trauma.Shared.BloodCult.Spells;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class BloodCultSpellsSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public static readonly VerbCategory BloodSpells = new("verb-categories-blood-cult",
        new SpriteSpecifier.Rsi(new("/Textures/_Trauma/BloodCult/actions.rsi"), "blood_spells"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultSpellComponent, ActionAttemptEvent>(OnCultSpellAttempt);
        SubscribeLocalEvent<CultSpellComponent, ActionValidateEvent>(OnCultSpellValidate);

        SubscribeLocalEvent<BloodCultSpellsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BloodCultistComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerbs);
        SubscribeLocalEvent<BloodCultSpellsComponent, RadialSelectorSelectedMessage>(OnSpellSelected);
        SubscribeLocalEvent<BloodCultSpellsComponent, CreateSpellDoAfterEvent>(OnSpellCreated);

        SubscribeLocalEvent<BloodCultShacklesEvent>(OnShackles);
        SubscribeLocalEvent<SummonEquipmentEvent>(OnSummonEquipment);
    }

    #region BaseHandlers

    private void OnCultSpellAttempt(Entity<CultSpellComponent> ent, ref ActionAttemptEvent args)
    {
        args.Cancelled |= _blocker.CanSpeak(args.User);
    }

    private void OnCultSpellValidate(Entity<CultSpellComponent> ent, ref ActionValidateEvent args)
    {
        if (ent.Comp.BypassProtection || args.Input.EntityTarget is not {} netTarget)
            return;

        var target = GetEntity(netTarget);

        // TODO: actual magic protection shit, show a popup
        if (HasComp<MindShieldComponent>(target))
            args.Invalid = true;
    }

    private void OnActionRemoved(Entity<BloodCultSpellsComponent> ent, ref ActionRemovedEvent args)
    {
        ent.Comp.SelectedSpells.Remove(args.Action);
        Dirty(ent);
    }

    private void OnStartup(Entity<BloodCultSpellsComponent> ent, ref ComponentStartup args)
    {
        _ui.SetUi(ent.Owner, RadialSelectorUiKey.Key, new InterfaceData("RadialSelectorMenuBUI", 0f, false));
    }

    private void OnGetVerbs(Entity<BloodCultistComponent> cultist, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (args.User != args.Target || _cult.GetSpells(cultist) is not {} spells)
            return;

        args.Verbs.Add(new ExamineVerb
        {
            Category = BloodSpells,
            Text = Loc.GetString("blood-cult-select-spells-verb"),
            Priority = 1,
            Act = () => SelectBloodSpells(spells, cultist)
        });
        args.Verbs.Add(new ExamineVerb
        {
            Category = BloodSpells,
            Text = Loc.GetString("blood-cult-remove-spells-verb"),
            Priority = 0,
            Act = () => RemoveBloodSpells(spells, cultist)
        });
    }

    private void OnSpellSelected(Entity<BloodCultSpellsComponent> ent, ref RadialSelectorSelectedMessage args)
    {
        var user = args.Actor;
        if (!ent.Comp.AddSpellsMode)
        {
            if (NetEntity.TryParse(args.SelectedItem, out var netAction))
                _actions.RemoveAction(user, GetEntity(netAction));

            return;
        }

        if (ent.Comp.SelectedSpells.Count >= ent.Comp.MaxSpells)
        {
            _popup.PopupClient(Loc.GetString("blood-cult-spells-too-many"), user, user, PopupType.Medium);
            return;
        }

        var createSpellEvent = new CreateSpellDoAfterEvent(args.SelectedItem);
        var doAfter = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.SpellCreationTime,
            createSpellEvent,
            eventTarget: ent)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnSpellCreated(Entity<BloodCultSpellsComponent> ent, ref CreateSpellDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled ||
            _actions.AddAction(args.User, args.ActionProtoId, container: ent) is not {} action)
            return;

        ent.Comp.SelectedSpells.Add(action);
        Dirty(ent);
    }

    #endregion

    #region SpellsHandlers

    private void OnShackles(BloodCultShacklesEvent ev)
    {
        if (ev.Handled)
            return;

        var cuffs = PredictedSpawnAtPosition(ev.ShacklesProto, Transform(ev.Target).Coordinates);
        if (!_cuffable.TryAddNewCuffs(ev.Performer, ev.Target, cuffs))
        {
            PredictedDel(cuffs);
            return;
        }

        _stun.TryKnockdown(ev.Target, ev.KnockdownDuration, true);
        _status.TryAddStatusEffect<MutedComponent>(ev.Target, "Muted", ev.MuteDuration, true);
        ev.Handled = true;
    }

    private void OnSummonEquipment(SummonEquipmentEvent ev)
    {
        if (ev.Handled)
            return;

        foreach (var (slot, protoId) in ev.Prototypes)
        {
            var entity = Spawn(protoId, _transform.GetMapCoordinates(ev.Performer));
            _hands.TryPickupAnyHand(ev.Performer, entity);
            if (!TryComp(entity, out ClothingComponent? _))
                continue;

            _inventory.TryUnequip(ev.Performer, slot);
            _inventory.TryEquip(ev.Performer, entity, slot, force: true);
        }

        ev.Handled = true;
    }

    #endregion

    #region Helpers

    private void SelectBloodSpells(Entity<BloodCultSpellsComponent> ent, EntityUid user)
    {
        if (ent.Comp.SelectedSpells.Count >= ent.Comp.MaxSpells)
        {
            _popup.PopupClient(Loc.GetString("blood-cult-spells-too-many"), user, user);
            return;
        }

        ent.Comp.AddSpellsMode = true;

        var radialList = new List<RadialSelectorEntry>();
        foreach (var spellId in ent.Comp.AvailableActions)
        {
            var entry = new RadialSelectorEntry
            {
                Prototype = spellId
            };

            radialList.Add(entry);
        }

        var state = new RadialSelectorState(radialList);
        _ui.SetUiState(ent.Owner, RadialSelectorUiKey.Key, state);
        _ui.TryToggleUi(ent.Owner, RadialSelectorUiKey.Key, user);
    }

    private void RemoveBloodSpells(Entity<BloodCultSpellsComponent> ent, EntityUid user)
    {
        if (ent.Comp.SelectedSpells.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("blood-cult-no-spells"), user, user);
            return;
        }

        ent.Comp.AddSpellsMode = false;

        var radialList = new List<RadialSelectorEntry>();
        foreach (var spell in ent.Comp.SelectedSpells)
        {
            var entry = new RadialSelectorEntry
            {
                Prototype = spell.ToString(),
                Icon = Comp<ActionComponent>(spell).Icon
            };

            radialList.Add(entry);
        }

        var state = new RadialSelectorState(radialList);

        _ui.SetUiState(ent.Owner, RadialSelectorUiKey.Key, state);
        _ui.TryToggleUi(ent.Owner, RadialSelectorUiKey.Key, user);
    }

    #endregion
}
