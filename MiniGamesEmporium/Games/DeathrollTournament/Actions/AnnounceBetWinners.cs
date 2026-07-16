using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;
using System.Collections.Generic;

/// <summary>Sends the configurable bet payout announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceBetWinners
{
    public static void Execute(string winner, long bettingPot, IReadOnlyList<string> betWinners, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var template = betWinners.Count == 1
            ? config.DeathrollTournament.Chat.AnnounceBetWinnerMessage
            : config.DeathrollTournament.Chat.AnnounceBetWinnersMessage;
        var msg = DeathrollMessageFormatter.Format(
            template,
            config,
            winner: winner,
            bettingPotOverride: bettingPot,
            betWinners: betWinners);
        chatQueue.Enqueue(msg);
    }
}
