// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Constructs;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ConstructComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public List<EntityUid> ActionEntities = new();
}
