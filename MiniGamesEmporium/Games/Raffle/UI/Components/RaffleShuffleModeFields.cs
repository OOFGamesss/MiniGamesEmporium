using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;

/// <summary>Renders the shuffle ticket numbers toggle shared by the start door and settings tab.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Components;
public static class RaffleShuffleModeFields
{
    public static void Draw(PluginConfiguration config, string imguiSuffix)
    {
        var cfg = config.Raffle;
        ImGui.TextDisabled("Ticket Numbers");
        ImGui.Spacing();
        var shuffle = cfg.ShuffleTicketNumbers;
        if (ImGui.Checkbox($"Shuffle Numbers On Close##RaffleShuffle_{imguiSuffix}", ref shuffle))
        {
            cfg.ShuffleTicketNumbers = shuffle;
            config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(
                "Players keep the number of tickets they bought, but which numbers they hold is randomised when you close the raffle.\n" +
                "Their numbers stay hidden - and cannot be sent - until then.\n" +
                "Tickets bought after the shuffle are added on the end; use Re-shuffle to mix them in.");
    }
}
