using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Handles keyword messages in enabled chat channels for BAR 777 queue enrolment.</summary>

namespace MiniGamesEmporium.Games.Bar777.Events;
public sealed class Bar777KeywordHandler : IChatKeywordHandler
{
    private readonly PluginConfiguration config;
    private readonly Bar777SessionService bar777SessionService;
    private readonly PlayerInfoService playerInfo;

    public Bar777KeywordHandler(PluginConfiguration config, Bar777SessionService bar777SessionService, PlayerInfoService playerInfo)
    {
        this.config         = config;
        this.bar777SessionService = bar777SessionService;
        this.playerInfo     = playerInfo;
    }

    public void TryHandleKeyword(SeString? sender, string message, XivChatType kind)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (!this.config.Bar777.UseQueue) return;
        if (this.bar777SessionService.IsQueuePaused) return;

        var queueName = KeywordMatcher.TryResolveJoiner(
            this.config.QueueJoinChannels, this.config.QueueKeyword, kind, sender, message, this.playerInfo);
        if (queueName == null) return;

        this.bar777SessionService.TryEnqueuePlayer(queueName);
    }
}
