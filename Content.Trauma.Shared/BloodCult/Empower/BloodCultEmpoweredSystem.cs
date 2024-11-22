// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Trauma.Shared.BloodCult.Spells;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.BloodCult.Empower;

public sealed partial class BloodCultEmpoweredSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private ITileDefinitionManager _tile = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedBloodCultSystem _cult = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultEmpoweredComponent, ComponentStartup>(OnEmpowerStartup);
        SubscribeLocalEvent<BloodCultEmpoweredComponent, ComponentShutdown>(OnEmpowerShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateTimers();
    }

    private void OnEmpowerStartup(Entity<BloodCultEmpoweredComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.EmpowerEnd = _timing.CurTime + ent.Comp.Duration;
        _alerts.ShowAlert(ent.Owner, ent.Comp.EmpoweredAlert);
        if (_cult.GetSpells(ent) is {} spells)
        {
            spells.Comp.MaxSpells += ent.Comp.ExtraSpells;
            Dirty(spells);
        }
    }

    private void OnEmpowerShutdown(Entity<BloodCultEmpoweredComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.EmpoweredAlert);
        // theres definitely some bug that can happen here but its probably hard to do
        if (_cult.GetSpells(ent) is {} spells)
        {
            spells.Comp.MaxSpells -= ent.Comp.ExtraSpells;
            Dirty(spells);
        }
    }

    private void UpdateTimers()
    {
        var query = EntityQueryEnumerator<BloodCultEmpoweredComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_net.IsClient && uid != _player.LocalEntity)
                continue; // only predict for yourself

            if (!_cult.IsCultist(uid))
            {
                RemCompDeferred(uid, comp);
                continue;
            }

            if (AnyCultTilesNearby((uid, comp)))
            {
                // keep it refreshed near cult tiles
                comp.EmpowerEnd = now + comp.Duration;
                continue;
            }

            if (now >= comp.EmpowerEnd)
                RemCompDeferred(uid, comp);
        }
    }

    private bool AnyCultTilesNearby(Entity<BloodCultEmpoweredComponent> ent)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not {} gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return false;

        var cultTile = _tile[ent.Comp.CultTile];
        var tileId = cultTile.TileId;

        var pos = xform.Coordinates.Position;
        var radius = ent.Comp.NearbyCultTileRadius;
        var tiles = _map.GetLocalTilesIntersecting(
            gridUid, grid,
            // TODO: theres a nicer version of this, Centered or something
            new Box2(pos + new Vector2(-radius, -radius), pos + new Vector2(radius, radius)));

        foreach (var tileRef in tiles)
        {
            if (tileRef.Tile.TypeId == tileId)
                return true;
        }

        return false;
    }
}
