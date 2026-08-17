using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Adds players who post the join keyword to the tournament while registration is open.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Events;
public sealed class DeathrollKeywordHandler : IChatKeywordHandler
{
    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private readonly PlayerInfoService playerInfo;

    public DeathrollKeywordHandler(PluginConfiguration config, DeathrollTournamentService deathrollService, PlayerInfoService playerInfo)
    {
        this.config           = config;
        this.deathrollService = deathrollService;
        this.playerInfo       = playerInfo;
    }

    public string GameName => DeathrollGameIds.DisplayName;

    public void TryHandleKeyword(SeString? sender, string message, XivChatType kind)
    {
        var cfg = this.config.DeathrollTournament;
        if (!cfg.AutoJoinKeyword) return;
        if (!this.deathrollService.IsSessionActive()) return;
        if (this.deathrollService.HasActiveTournament()) return;

        var queueName = KeywordMatcher.TryResolveJoiner(
            cfg.JoinChannels, cfg.JoinKeyword, kind, sender, message, this.playerInfo);
        if (queueName == null) return;

        this.deathrollService.AddPlayer(queueName);
    }
}
