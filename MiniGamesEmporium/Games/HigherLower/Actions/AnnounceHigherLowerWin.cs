using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured win shout message for a Higher/Lower session winner, including their share of the pot.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class AnnounceHigherLowerWin
{
    public static void Execute(string playerName, int rounds, long totalPot, long winningAmount, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = HigherLowerMessageFormatter.Format(
            config.HigherLower.Chat.WinShoutMessage,
            config,
            playerName: playerName,
            rounds: rounds,
            totalPotOverride: totalPot,
            winningAmount: winningAmount);
        chatQueue.Enqueue(msg);
    }
}
