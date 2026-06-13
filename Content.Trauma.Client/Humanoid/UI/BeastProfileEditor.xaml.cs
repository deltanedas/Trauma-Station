// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Prototypes;
using Content.Trauma.Common.Humanoid;

namespace Content.Trauma.Client.Humanoid.UI;

[GenerateTypedNameReferences]
public sealed partial class BeastProfileEditor : BoxContainer
{
    private readonly IPrototypeManager _proto;

    public event Action<BeastProfile>? OnSave;

    private BeastProfile? _profile;

    public BeastProfileEditor(IPrototypeManager proto)
    {
        RobustXamlLoader.Load(this);

        _proto = proto;

        /*SaveButton.OnPressed += _ =>
        {
            if (_profile is not { } profile)
                return;

            OnSave?.Invoke(profile);
            //_modified = false;
            //SaveButton.Disabled = true;
        };*/
    }

    public void SetProfile(ProtoId<SpeciesPrototype> species, BeastProfile? profile)
    {
        _profile = profile;
        Visible = _proto.Index(species).Beast;
        // TODO
    }
}
