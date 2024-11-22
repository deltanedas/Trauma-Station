// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.BloodCult.Items.ShuttleCurse;

public abstract partial class SharedShuttleCurseSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleCurseComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(Entity<ShuttleCurseComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.User;
        var curseProvider = EnsureCurseProvider(ent);
        if (EnsureCurseProvider(ent) is not {} provider)
        {
            Popup.PopupClient(Loc.GetString("shuttle-curse-cant-activate"), ent, user, PopupType.MediumCaution);
            return;
        }

        if (provider.Comp.CurrentUses >= provider.Comp.MaxUses)
        {
            Popup.PopupClient(Loc.GetString("shuttle-curse-max-charges"), ent, user, PopupType.MediumCaution);
            return;
        }

        DelayShuttle(ent, provider, user);
    }

    protected virtual void DelayShuttle(Entity<ShuttleCurseComponent> ent, Entity<ShuttleCurseProviderComponent> provider, EntityUid user)
    {
        // emergency shuttle / round end cant be predicted :(
    }

    private Entity<ShuttleCurseProviderComponent>? EnsureCurseProvider(EntityUid uid)
        // TODO: store this dogshit on the gamerule not map
        => Transform(uid).MapUid is {} map
            ? (map, EnsureComp<ShuttleCurseProviderComponent>(map))
            : null;
}
