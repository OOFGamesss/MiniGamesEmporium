using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable matchup announcement message to chat, substituting the two players currently up in the bracket.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceMatchup
{
    public static void Execute(string player1, string player2, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.AnnounceMatchupMessage,
            config,
            player1: player1,
            player2: player2);
        chatQueue.Enqueue(msg);
    }
}
