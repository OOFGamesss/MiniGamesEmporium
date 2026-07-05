using System;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Services;

/// <summary>Handles RandomNumber roll events for BAR 777, routing them to payment verification or session roll recording.</summary>

namespace MiniGamesEmporium.Games.Bar777.Events;
public sealed class Bar777RollHandler : IChatRollHandler
{
    private readonly PluginConfiguration config;
    private readonly Bar777SessionService bar777SessionService;

    public Bar777RollHandler(PluginConfiguration config, Bar777SessionService bar777SessionService)
    {
        this.config         = config;
        this.bar777SessionService = bar777SessionService;
    }

    public void TryHandleRoll(string playerName, int rollValue, int rollMax)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (rollMax > 0) return;
        if (string.IsNullOrEmpty(playerName)) return;
        if (!session.PaymentVerified)
        {
            this.bar777SessionService.TryCatchPaymentRoll(playerName, rollValue);
            return;
        }
        if (!playerName.Equals(session.PlayerName, StringComparison.OrdinalIgnoreCase)) return;
        this.bar777SessionService.RecordRoll(rollValue);
    }
}
