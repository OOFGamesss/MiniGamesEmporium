using System;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Services;

/// <summary>Handles Coin Collector dice rolls made by the player currently taking their turn.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Events;
public sealed class CoinCollectorRollHandler : IChatRollHandler
{
    private readonly CoinCollectorService coinCollectorService;

    public CoinCollectorRollHandler(CoinCollectorService coinCollectorService)
    {
        this.coinCollectorService = coinCollectorService;
    }

    public string GameName => CoinCollectorGameIds.DisplayName;

    public void TryHandleRoll(string playerName, int rollValue, int rollMax)
    {
        var session = this.coinCollectorService.GetActiveSession();
        if (session == null || !session.PaymentVerified) return;
        if (!NamesMatch(playerName, session.PlayerName)) return;
        this.coinCollectorService.RecordRoll(rollValue, rollMax);
    }

    private static bool NamesMatch(string senderName, string sessionPlayer)
    {
        if (string.IsNullOrWhiteSpace(senderName) || string.IsNullOrWhiteSpace(sessionPlayer)) return false;
        return PlayerInfoService.StripWorld(sessionPlayer)
            .Equals(PlayerInfoService.StripWorld(senderName), StringComparison.OrdinalIgnoreCase);
    }
}
