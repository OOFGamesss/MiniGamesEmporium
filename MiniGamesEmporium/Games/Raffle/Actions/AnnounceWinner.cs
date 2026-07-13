using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable winner announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class AnnounceWinner
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue, string winner, int winningNumber, long totalPot)
    {
        var msg = RaffleMessageFormatter.Format(
            config.Raffle.Chat.AnnounceWinnerMessage, config,
            winner: winner, winningNumber: winningNumber, totalPotOverride: totalPot);
        chatQueue.Enqueue(msg);
    }
}
