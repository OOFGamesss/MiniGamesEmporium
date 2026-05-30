using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Events;
using MiniGamesEmporium.Utility;
using System.Collections.Generic;

/// <summary>Subscribes to the Dalamud chat message event and dispatches roll and keyword events to registered per-game handlers.</summary>

namespace MiniGamesEmporium.Services;
public sealed class ChatListener : IDisposable
{
    private readonly IChatGui chatGui;
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly IReadOnlyList<IChatRollHandler> rollHandlers;
    private readonly IReadOnlyList<IChatKeywordHandler> keywordHandlers;
    private readonly IPluginLog log;

    public ChatListener(
        IChatGui chatGui,
        PluginConfiguration config,
        SessionService sessionService,
        IReadOnlyList<IChatRollHandler> rollHandlers,
        IReadOnlyList<IChatKeywordHandler> keywordHandlers,
        IPluginLog log)
    {
        this.chatGui         = chatGui;
        this.config          = config;
        this.sessionService  = sessionService;
        this.rollHandlers    = rollHandlers;
        this.keywordHandlers = keywordHandlers;
        this.log             = log;
        this.chatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        this.chatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (this.sessionService.IsPaused) return;
        var messageText = message.Message?.TextValue ?? string.Empty;
        var kind = message.LogKind;
        if (string.IsNullOrEmpty(messageText)) return;
        this.log.Debug($"[{kind}({(ushort)kind})] '{messageText}'");

        if (kind == XivChatType.RandomNumber)
        {
            if (message.Message == null) return;
            if (!RollParser.TryParse(message.Message, out var playerName, out var rollValue, out var rollMax)) return;
            this.log.Information($"Roll: {playerName} rolled {rollValue} (max: {rollMax})");
            foreach (var handler in this.rollHandlers)
                handler.TryHandleRoll(playerName, rollValue, rollMax);
            return;
        }

        if (kind == XivChatType.SystemMessage) return;

        var listen = this.config.QueueJoinChannels;
        if (!listen.AnyEnabled()) return;
        if (!IsEnqueueChatKind(kind, listen)) return;
        foreach (var handler in this.keywordHandlers)
            handler.TryHandleKeyword(message.Sender, messageText);
    }

    private static bool IsEnqueueChatKind(XivChatType kind, QueueJoinChannelsConfig listen) =>
        kind switch
        {
            XivChatType.Say          => listen.Say,
            XivChatType.Shout        => listen.Shout,
            XivChatType.Yell         => listen.Yell,
            XivChatType.TellIncoming => listen.TellIncoming,
            _                        => false,
        };
}
