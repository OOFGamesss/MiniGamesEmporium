using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable match-win announcement to chat when a player wins the full best-of series and eliminates their opponent.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceMatchWin
{
    public static void Execute(string matchWinner, string matchLoser, int winnerWins, int loserWins, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var score = $"{winnerWins} - {loserWins}";
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.AnnounceMatchWinMessage,
            config,
            matchWinner: matchWinner,
            matchLoser:  matchLoser,
            roundScore:  score);
        chatQueue.Enqueue(msg);
    }
}
