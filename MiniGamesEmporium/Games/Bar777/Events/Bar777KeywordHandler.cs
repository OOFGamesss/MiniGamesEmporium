using Dalamud.Game.Text.SeStringHandling;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Events;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;
using System;

/// <summary>Handles keyword messages in enabled chat channels for BAR 777 queue enrolment.</summary>

namespace MiniGamesEmporium.Games.Bar777.Events;
public sealed class Bar777KeywordHandler : IChatKeywordHandler
{
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;

    public Bar777KeywordHandler(PluginConfiguration config, SessionService sessionService)
    {
        this.config         = config;
        this.sessionService = sessionService;
    }

    public void TryHandleKeyword(SeString? sender, string message)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (!this.config.Bar777.UseQueue) return;
        if (this.sessionService.IsQueuePaused) return;
        if (string.IsNullOrWhiteSpace(this.config.QueueKeyword)) return;
        if (!message.Contains(this.config.QueueKeyword, StringComparison.OrdinalIgnoreCase)) return;
        if (sender == null) return;

        var queueName = PlayerNameHelper.BuildQueueName(sender);
        if (string.IsNullOrWhiteSpace(queueName)) return;

        var localName = MiniGamesEmporium.ObjectTable.LocalPlayer?.Name.TextValue;
        if (!string.IsNullOrEmpty(localName))
        {
            var at = queueName.IndexOf('@');
            var senderName = at < 0 ? queueName : queueName[..at];
            if (senderName.Equals(localName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        this.sessionService.TryEnqueuePlayer(queueName);
    }
}
