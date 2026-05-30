using MiniGamesEmporium.Config;
using MiniGamesEmporium.Events;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Services;
using System;

/// <summary>Handles RandomNumber roll events for BAR 777, routing them to payment verification or session roll recording.</summary>

namespace MiniGamesEmporium.Games.Bar777.Events;
public sealed class Bar777RollHandler : IChatRollHandler
{
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;

    public Bar777RollHandler(PluginConfiguration config, SessionService sessionService)
    {
        this.config         = config;
        this.sessionService = sessionService;
    }

    public void TryHandleRoll(string playerName, int rollValue, int rollMax)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (rollMax > 0) return;
        if (string.IsNullOrEmpty(playerName)) return;
        if (!session.PaymentVerified)
        {
            this.sessionService.TryCatchPaymentRoll(playerName, rollValue);
            return;
        }
        if (!playerName.Equals(session.PlayerName, StringComparison.OrdinalIgnoreCase)) return;
        this.sessionService.RecordRoll(rollValue);
    }
}
