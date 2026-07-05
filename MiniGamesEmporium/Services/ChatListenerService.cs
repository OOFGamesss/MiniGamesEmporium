using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Services;

/// <summary>Dispatches chat roll and keyword events to the registered per-game handlers.</summary>

namespace MiniGamesEmporium.Services;
public sealed class ChatListenerService : IDisposable
{
    private readonly IChatGui chatGui;
    private readonly PluginConfiguration config;
    private readonly Bar777SessionService bar777SessionService;
    private readonly IReadOnlyList<IChatRollHandler> rollHandlers;
    private readonly IReadOnlyList<IChatKeywordHandler> keywordHandlers;
    private readonly RollService rollService;
    private readonly IPluginLog log;

    public ChatListenerService(
        IChatGui chatGui,
        PluginConfiguration config,
        Bar777SessionService bar777SessionService,
        IReadOnlyList<IChatRollHandler> rollHandlers,
        IReadOnlyList<IChatKeywordHandler> keywordHandlers,
        RollService rollService,
        IPluginLog log)
    {
        this.chatGui         = chatGui;
        this.config          = config;
        this.bar777SessionService  = bar777SessionService;
        this.rollHandlers    = rollHandlers;
        this.keywordHandlers = keywordHandlers;
        this.rollService     = rollService;
        this.log             = log;
        this.chatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        this.chatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (this.bar777SessionService.IsPaused) return;
        var messageText = message.Message?.TextValue ?? string.Empty;
        var kind = message.LogKind;
        if (string.IsNullOrEmpty(messageText)) return;
        this.log.Debug($"[{kind}({(ushort)kind})] '{messageText}'");

        if (kind is XivChatType.RandomNumber or XivChatType.Party or XivChatType.Alliance)
        {
            if (message.Message != null
                && this.rollService.TryParseRoll(kind, message.Message, message.Sender, out var roll))
            {
                this.log.Information($"Roll: {roll.PlayerName} rolled {roll.RollValue} (max: {roll.RollMax})");
                foreach (var handler in this.rollHandlers)
                    handler.TryHandleRoll(roll.PlayerName, roll.RollValue, roll.RollMax);
            }
            return;
        }

        if (kind == XivChatType.SystemMessage) return;

        if (!IsKeywordChatKind(kind)) return;
        foreach (var handler in this.keywordHandlers)
            handler.TryHandleKeyword(message.Sender, messageText, kind);
    }

    private static bool IsKeywordChatKind(XivChatType kind) =>
        kind is XivChatType.Say or XivChatType.Shout or XivChatType.Yell or XivChatType.TellIncoming;
}

public interface IChatRollHandler
{
    void TryHandleRoll(string playerName, int rollValue, int rollMax);
}

public interface IChatKeywordHandler
{
    void TryHandleKeyword(SeString? sender, string message, XivChatType kind);
}
