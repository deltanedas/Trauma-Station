// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Humanoid;

/// <summary>
/// Category for grouping <see cref="BeastTraitPrototype"/> in the UI.
/// </summary>
[Prototype]
public sealed partial class BeastTraitCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Human readable name of this category.
    /// </summary>
    [DataField(required: true)]
    public string Name;
}
