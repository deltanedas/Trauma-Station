// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.BloodCult.UI;

namespace Content.Trauma.Client.BloodCult.NameSelector;

public sealed class NameSelectorBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    protected override void Open()
    {
        base.Open();

        var window = this.CreateWindow<NameSelectorWindow>();
        window.OnSelected += name =>
        {
            SendPredictedMessage(new NameSelectedMessage(name));
            Close();
        };
        window.OpenCentered();
    }
}
