// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Constructs;

namespace Content.Trauma.Server.BloodCult.Constructs;

public sealed class ConstructSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConstructComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ConstructComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<ConstructComponent> ent, ref MapInitEvent args)
    {
        var query = EntityQueryEnumerator<BloodCultRuleComponent>();
        while (query.MoveNext(out _, out var rule))
        {
            rule.Constructs.Add(ent);
        }
    }

    private void OnShutdown(Entity<ConstructComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<BloodCultRuleComponent>();
        while (query.MoveNext(out _, out var rule))
        {
            rule.Constructs.Remove(ent);
        }
    }
}
