using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable round-win announcement to chat when a player wins a game within a best-of series but the match is not yet decided.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceRoundWin
{
    public static void Execute(string roundWinner, int winnerWins, int loserWins, int gamesLeft, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var score = $"{winnerWins} - {loserWins}";
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.AnnounceRoundWinMessage,
            config,
            roundWinner: roundWinner,
            roundScore:  score,
            roundsLeft:  gamesLeft);
        chatQueue.Enqueue(msg);
    }
}
