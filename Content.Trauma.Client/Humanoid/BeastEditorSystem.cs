// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Trauma.Client.Humanoid.UI;

namespace Content.Trauma.Client.Humanoid;

/// <summary>
/// Adds the beastmen editor tab to character editor, shown when the right species is selected.
/// </summary>
public sealed partial class BeastEditorSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        LobbyUIController.OnProfileEditorCreated += AddProfileEditorTab;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        LobbyUIController.OnProfileEditorCreated -= AddProfileEditorTab;
    }

    private void AddProfileEditorTab(HumanoidProfileEditor editor)
    {
        // place it after markings tab
        var below = editor.MarkingsTab;
        var index = below.GetPositionInParent() + 1;

        var tab = new BeastProfileEditor(_proto);
        tab.OnSave += beast =>
        {
            editor.Profile = editor.Profile?.WithBeast(beast);
            editor.IsDirty = true;
        };

        editor.OnSetProfile += profile =>
        {
            if (profile is not null)
                tab.SetProfile(profile.Species, profile.Beast);
        };
        editor.TabContainer.AddChild(tab);
        tab.SetPositionInParent(index);
        TabContainer.SetTabTitle(tab, "Beastmen");
    }
}
