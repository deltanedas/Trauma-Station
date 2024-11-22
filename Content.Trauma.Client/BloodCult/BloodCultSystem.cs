// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Trauma.Shared.BloodCult;
using Content.Trauma.Shared.BloodCult.Components;
using Content.Trauma.Shared.BloodCult.Constructs;
using Robust.Shared.Random;

namespace Content.Trauma.Client.BloodCult;

public sealed partial class BloodCultSystem : SharedBloodCultSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public static readonly ProtoId<FactionIconPrototype> CultistIcon = "BloodCultMember";
    public static readonly ProtoId<FactionIconPrototype> LeaderIcon = "BloodCultLeader";

    public override void Initialize()
    {
        SubscribeLocalEvent<PentagramComponent, ComponentStartup>(OnPentagramAdded);
        SubscribeLocalEvent<PentagramComponent, ComponentShutdown>(OnPentagramRemoved);

        SubscribeLocalEvent<BloodCultMemberComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnPentagramAdded(EntityUid uid, PentagramComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || sprite.LayerMapTryGet(PentagramKey.Key, out _))
            return;

        var adj = sprite.Bounds.Height / 2 + 1.0f / 32 * 10.0f;

        var randomState = _random.Pick(component.States);

        var layer = _sprite.AddLayer((uid, sprite), new SpriteSpecifier.Rsi(component.RsiPath, randomState));

        _sprite.LayerMapSet((uid, sprite), PentagramKey.Key, layer);
        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(0.0f, adj));
    }

    private void OnPentagramRemoved(EntityUid uid, PentagramComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.LayerMapTryGet(PentagramKey.Key, out var layer))
            return;

        _sprite.RemoveLayer((uid, sprite), layer);
    }

    private void OnGetStatusIcons(Entity<BloodCultMemberComponent> ent, ref GetStatusIconsEvent args)
    {
        var id = HasComp<BloodCultLeaderComponent>(ent)
            ? LeaderIcon
            : CultistIcon;
        if (_proto.Resolve(id, out var icon))
            args.StatusIcons.Add(icon);
    }
}
