using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Sends the bracket announcement and the current round's matchup list.</summary>

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
            chatQueue.Enqueue($"{channel}  {PlayerInfoService.StripWorld(match.Player1)} vs {PlayerInfoService.StripWorld(match.Player2)}");
        }
    }

    private static string ExtractChannel(string message)
    {
        if (string.IsNullOrEmpty(message) || message[0] != '/') return "/say";
        var space = message.IndexOf(' ');
        return space > 0 ? message[..space] : "/say";
    }
}
