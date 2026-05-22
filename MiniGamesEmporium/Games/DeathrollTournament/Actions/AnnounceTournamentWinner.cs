using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable tournament winner announcement message to chat, substituting the winning player name and final pot.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceTournamentWinner
{
    public static void Execute(string winner, long totalPot, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.AnnounceTournamentWinnerMessage,
            config,
            winner: winner,
            totalPotOverride: totalPot);
        chatQueue.Enqueue(msg);
    }
}
