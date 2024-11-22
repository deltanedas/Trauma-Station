// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.BloodCult.Runes;

namespace Content.Trauma.Client.BloodCult.Runes.UI;

public sealed partial class RuneDrawerBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _proto = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(GetButtons());
        _menu.OpenOverMouseScreenPosition();
    }

    private List<RadialMenuOptionBase> GetButtons()
    {
        var selectors = _proto.EnumeratePrototypes<RuneSelectorPrototype>()
            .OrderBy(r => r.ID)
            .ToList();

        var options = new List<RadialMenuOptionBase>(selectors.Count);
        foreach (var selector in selectors)
        {
            if (!_proto.Resolve(selector.Prototype, out var rune))
                continue;

            options.Add(new RadialMenuActionOption<ProtoId<RuneSelectorPrototype>>(OnSelected, selector.ID)
            {
                ToolTip = rune.Name,
                IconSpecifier = RadialMenuIconSpecifier.With(selector.Prototype)
            });
        }

        return options;
    }

    private void OnSelected(ProtoId<RuneSelectorPrototype> id)
    {
        SendPredictedMessage(new RuneDrawerSelectedMessage(id));
        Close();
    }
}
