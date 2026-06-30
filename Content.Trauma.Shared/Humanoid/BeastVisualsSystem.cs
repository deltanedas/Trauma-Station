// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Trauma.Common.Humanoid;

namespace Content.Trauma.Shared.Humanoid;

public sealed partial class BeastVisualsSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeastVisualsComponent, BeastProfileChangedEvent>(OnProfileChanged);
    }

    private void OnProfileChanged(Entity<BeastVisualsComponent> ent, ref BeastProfileChangedEvent args)
    {
        if (args.Profile is not { } beast)
            return;

        foreach (var organ in _body.GetOrgans<VisualOrganComponent>(ent.Owner))
        {
            if (_body.GetCategory(organ.Owner) is not { } category)
                continue;

            var phenoId = beast.Phenotypes[beast.OrganIndices.GetValueOrDefault(category)];
            if (!ProtoMan.Resolve<BeastPhenotypePrototype>(phenoId, out var pheno))
                continue;

            // can't just check if it's an interal organ thanks to eyes using parts.rsi :D
            var rsi = organ.Comp.Data.RsiPath.EndsWith("organs.rsi") ? pheno.OrgansRsi : pheno.PartsRsi;
            organ.Comp.Data.RsiPath = rsi;
            Dirty(organ);
        }
    }
}
