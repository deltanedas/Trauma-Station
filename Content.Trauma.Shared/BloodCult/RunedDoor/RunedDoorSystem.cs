// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Doors;
using Content.Shared.Prying.Components;
using Content.Trauma.Shared.Repulse;

namespace Content.Trauma.Shared.BloodCult.RunedDoor;

public sealed partial class RunedDoorSystem : EntitySystem
{
    [Dependency] private SharedBloodCultSystem _cult = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RunedDoorComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<RunedDoorComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<RunedDoorComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<RunedDoorComponent, RepulseAttemptEvent>(OnRepulseAttempt);
    }

    private void OnBeforeDoorOpened(Entity<RunedDoorComponent> door, ref BeforeDoorOpenedEvent args)
    {
        if (args.User is not { } user)
            return;

        if (!_cult.IsCultist(user))
            args.Cancel();
    }

    private void OnBeforeDoorClosed(Entity<RunedDoorComponent> door, ref BeforeDoorClosedEvent args)
    {
        if (args.User is not { } user)
            return;

        if (!_cult.IsCultist(user))
            args.Cancel();
    }

    private void OnBeforePry(Entity<RunedDoorComponent> door, ref BeforePryEvent args)
    {
        args.Cancelled = true;
    }

    private void OnRepulseAttempt(Entity<RunedDoorComponent> door, ref RepulseAttemptEvent args)
    {
        if (_cult.IsCultist(args.Target))
            args.Cancelled = true;
    }
}
