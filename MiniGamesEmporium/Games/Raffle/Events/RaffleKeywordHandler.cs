using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Adds players who post the join keyword to the raffle while a session is open. Tickets are granted only on payment.</summary>

namespace MiniGamesEmporium.Games.Raffle.Events;
public sealed class RaffleKeywordHandler : IChatKeywordHandler
{
    private readonly PluginConfiguration config;
    private readonly RaffleService raffleService;
    private readonly PlayerInfoService playerInfo;

    public RaffleKeywordHandler(PluginConfiguration config, RaffleService raffleService, PlayerInfoService playerInfo)
    {
        this.config        = config;
        this.raffleService = raffleService;
        this.playerInfo    = playerInfo;
    }

    public void TryHandleKeyword(SeString? sender, string message, XivChatType kind)
    {
        var cfg = this.config.Raffle;
        if (!cfg.AutoJoinKeyword) return;
        if (!this.raffleService.IsSessionActive()) return;

        var queueName = KeywordMatcher.TryResolveJoiner(
            cfg.JoinChannels, cfg.JoinKeyword, kind, sender, message, this.playerInfo);
        if (queueName == null) return;

        this.raffleService.AddPlayer(queueName);
    }
}
