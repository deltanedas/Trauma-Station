// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.AlertLevel;

namespace Content.Client.Communications.UI;

public sealed partial class CommunicationsConsoleMenu
{
    public void SetAlertLevel(AlertLevelSystem sys)
    {
        AlertLevelControls.AlertLevel = sys;
    }

    public void SetStation(EntityUid station)
    {
        AlertLevelControls.Station = station;
        AlertLevelControls.UpdateUnlock();
    }
}
