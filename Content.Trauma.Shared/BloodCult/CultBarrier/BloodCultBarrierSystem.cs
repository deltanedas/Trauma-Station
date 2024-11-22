// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Trauma.Shared.BloodCult.Runes;

namespace Content.Trauma.Shared.BloodCult.CultBarrier;

public sealed partial class BloodCultBarrierSystem : EntitySystem
{
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<RuneDrawerComponent> _drawerQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultBarrierComponent, InteractUsingEvent>(OnInteract);
    }

    private void OnInteract(Entity<BloodCultBarrierComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        if (args.Handled || !_drawerQuery.HasComp(args.Used) || !_cult.IsCultist(user))
            return;

        _popup.PopupClient("You tap the barrier with your dagger and it vanishes.", user, user);
        PredictedQueueDel(ent);
        args.Handled = true;
    }
}
