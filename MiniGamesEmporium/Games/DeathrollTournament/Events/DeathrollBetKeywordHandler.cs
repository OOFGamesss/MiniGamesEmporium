using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Adds bets declared via the bet keyword while registration is open.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Events;
public sealed class DeathrollBetKeywordHandler : IChatKeywordHandler
{
    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private readonly DeathrollBettingService bettingService;
    private readonly PlayerInfoService playerInfo;

    public DeathrollBetKeywordHandler(PluginConfiguration config, DeathrollTournamentService deathrollService, DeathrollBettingService bettingService, PlayerInfoService playerInfo)
    {
        this.config           = config;
        this.deathrollService = deathrollService;
        this.bettingService   = bettingService;
        this.playerInfo       = playerInfo;
    }

    public string GameName => DeathrollGameIds.DisplayName;

    public void TryHandleKeyword(SeString? sender, string message, XivChatType kind)
    {
        var cfg = this.config.DeathrollTournament;
        if (!cfg.AutoBetKeyword) return;
        if (!this.deathrollService.IsSessionActive()) return;
        if (this.deathrollService.HasActiveTournament()) return;
        if (!this.bettingService.IsBettingEnabledForSession()) return;

        var result = KeywordMatcher.TryResolveBet(
            cfg.BetChannels, cfg.BetKeyword, kind, sender, message, this.playerInfo, cfg.RegisteredPlayers);
        if (result == null) return;

        if (result.ResolvedTargetName != null)
            this.bettingService.DeclareBet(result.BettorQueueName, result.ResolvedTargetName, fromChat: true);
        else
            this.bettingService.DeclareUnresolvedBet(result.BettorQueueName, result.RawTargetText);
    }
}
