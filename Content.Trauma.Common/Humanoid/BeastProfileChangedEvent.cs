// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Humanoid;

/// <summary>
/// Event raised on a mob when its <see cref="BeastProfile"/> is changed.
/// </summary>
[ByRefEvent]
public record struct BeastProfileChangedEvent(BeastProfile? Profile);
