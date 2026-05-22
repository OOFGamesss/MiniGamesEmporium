using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable bracket announcement followed by a numbered matchup list for the current round.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceBracket
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue, DeathrollTournamentState state)
    {
        var header  = DeathrollMessageFormatter.Format(config.DeathrollTournament.Chat.AnnounceBracketMessage, config);
        var channel = ExtractChannel(header);
        chatQueue.Enqueue(header);

        var round = state.Rounds[state.CurrentRoundIndex];
        foreach (var match in round)
        {
            if (string.IsNullOrEmpty(match.Player1) || string.IsNullOrEmpty(match.Player2)) continue;
            if (DeathrollGameIds.IsBye(match.Player1) || DeathrollGameIds.IsBye(match.Player2)) continue;
            chatQueue.Enqueue($"{channel}  {StripWorld(match.Player1)} vs {StripWorld(match.Player2)}");
        }
    }

    private static string ExtractChannel(string message)
    {
        if (string.IsNullOrEmpty(message) || message[0] != '/') return "/say";
        var space = message.IndexOf(' ');
        return space > 0 ? message[..space] : "/say";
    }

    public static string GetRoundLabel(DeathrollTournamentState state)
    {
        var idx = state.CurrentRoundIndex;
        return idx == state.Rounds.Count - 1 ? "The Final" : $"Round {idx + 1}";
    }

    private static string StripWorld(string entry)
    {
        var at = entry.IndexOf('@');
        return at >= 0 ? entry[..at].Trim() : entry.Trim();
    }
}
