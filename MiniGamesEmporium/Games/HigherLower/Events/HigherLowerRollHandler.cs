using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.Services;

/// <summary>Handles Higher/Lower /dice rolls from the host for capture.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Events;
public sealed class HigherLowerRollHandler : IChatRollHandler
{
    private readonly PluginConfiguration config;
    private readonly HigherLowerService higherLowerService;
    private readonly PlayerInfoService playerInfo;

    public HigherLowerRollHandler(PluginConfiguration config, HigherLowerService higherLowerService, PlayerInfoService playerInfo)
    {
        this.config              = config;
        this.higherLowerService  = higherLowerService;
        this.playerInfo          = playerInfo;
    }

    public void TryHandleRoll(string playerName, int rollValue, int rollMax)
    {
        var session = this.config.ActiveSession;
        if (session == null || !HigherLowerGameIds.Matches(session.GameName)) return;
        if (!session.PaymentVerified) return;
        if (rollMax != this.config.HigherLower.DiceSides) return;
        if (!this.playerInfo.IsHost(playerName)) return;
        this.higherLowerService.RecordRoll(rollValue);
    }
}
