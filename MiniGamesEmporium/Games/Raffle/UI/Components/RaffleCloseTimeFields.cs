using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Renders the raffle closing time fields using the shared close-time component.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Components;
public static class RaffleCloseTimeFields
{
    public static void DrawConfig(PluginConfiguration config, string suffix, float fieldWidth)
    {
        var cfg = config.Raffle;
        var hour   = cfg.CloseHour;
        var minute = cfg.CloseMinute;
        CloseTimeFields.Draw(suffix, fieldWidth, ref hour, ref minute, () =>
        {
            cfg.CloseHour   = hour;
            cfg.CloseMinute = minute;
            config.Save();
        });
        cfg.CloseHour   = hour;
        cfg.CloseMinute = minute;
    }
}
