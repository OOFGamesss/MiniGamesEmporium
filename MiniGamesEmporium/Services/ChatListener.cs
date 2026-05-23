using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Config;
using System;
using System.Linq;

/// <summary>Subscribes to the Dalamud chat message event to detect BAR 777 rolls, Deathroll Tournament rolls, and enqueue players who post the configured queue join keyword.</summary>

namespace MiniGamesEmporium.Services;
public sealed class ChatListener : IDisposable
{
    private readonly IChatGui chatGui;
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly IPluginLog log;
    public ChatListener(IChatGui chatGui, PluginConfiguration config, SessionService sessionService, DeathrollTournamentService deathrollService, IPluginLog log)
    {
        this.chatGui          = chatGui;
        this.config           = config;
        this.sessionService   = sessionService;
        this.deathrollService = deathrollService;
        this.log              = log;
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
        this.log.Debug($"[MGE] [{kind}({(ushort)kind})] '{messageText}'");
        if (kind == XivChatType.RandomNumber)
        {
            if (message.Message == null) return;
            TryHandleBar777Roll(message.Message);
            TryHandleDeathrollRoll(message.Message);
            return;
        }
        if (kind == XivChatType.SystemMessage)
            return;
        var listen = this.config.QueueJoinChannels;
        if (!listen.AnyEnabled()) return;
        if (!IsEnqueueChatKind(kind, listen)) return;
        TryHandleQueueKeyword(message.Sender, messageText);
    }
    private static bool IsEnqueueChatKind(XivChatType kind, QueueJoinChannelsConfig listen)
    {
        return kind switch
        {
            XivChatType.Say          => listen.Say,
            XivChatType.Shout        => listen.Shout,
            XivChatType.Yell         => listen.Yell,
            XivChatType.TellIncoming => listen.TellIncoming,
            _ => false,
        };
    }
    private void TryHandleBar777Roll(SeString seString)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName) || !session.PaymentVerified) return;
        if (!TryParseRoll(seString, out var playerName, out var rollValue, out var rollMax)) return;
        if (rollMax > 0) return;
        if (string.IsNullOrEmpty(playerName)) return;
        if (!playerName.Equals(session.PlayerName, StringComparison.OrdinalIgnoreCase)) return;
        this.sessionService.RecordRoll(rollValue);
        this.log.Information($"[MGE] Roll recorded: {rollValue} for {session.PlayerName}");
    }
    private void TryHandleDeathrollRoll(SeString seString)
    {
        if (!this.deathrollService.HasActiveTournament()) return;
        if (!TryParseRoll(seString, out var playerName, out var rollValue, out var rollMax)) return;
        if (rollMax == 0) return;
        if (string.IsNullOrEmpty(playerName))
        {
            var localName = MiniGamesEmporium.ObjectTable.LocalPlayer?.Name.TextValue;
            if (!string.IsNullOrEmpty(localName))
                playerName = localName;
        }
        if (!this.deathrollService.TryCatchNextMatchOrderRoll(playerName, rollValue, rollMax) &&
            !this.deathrollService.TryCatchNextGameOrderRoll(playerName, rollValue, rollMax))
            this.deathrollService.TryRecordRoll(playerName, rollValue, rollMax);
    }
    // Extracts the roller's name and the roll numbers from a RandomNumber SeString using payloads,
    // so detection is independent of the client's display language.
    private static bool TryParseRoll(SeString seString, out string playerName, out int rollValue, out int rollMax)
    {
        playerName = string.Empty;
        rollValue  = 0;
        rollMax    = 0;
        var playerPayload = seString.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        if (playerPayload != null)
            playerName = playerPayload.PlayerName;
        var lastText = seString.Payloads.OfType<TextPayload>().LastOrDefault();
        if (lastText?.Text == null) return false;
        return TryExtractNumbers(lastText.Text, out rollValue, out rollMax);
    }
    private static bool TryExtractNumbers(string text, out int first, out int second)
    {
        first  = 0;
        second = 0;
        var idx   = 0;
        var found = 0;
        while (idx < text.Length && found < 2)
        {
            while (idx < text.Length && !char.IsDigit(text[idx])) idx++;
            if (idx >= text.Length) break;
            var start = idx;
            while (idx < text.Length && char.IsDigit(text[idx])) idx++;
            if (int.TryParse(text.AsSpan(start, idx - start), out var num))
            {
                if (found == 0) first  = num;
                else            second = num;
                found++;
            }
        }
        return found > 0;
    }
    private void TryHandleQueueKeyword(SeString? sender, string message)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (!this.config.Bar777.UseQueue) return;
        if (string.IsNullOrWhiteSpace(this.config.QueueKeyword)) return;
        if (!message.Contains(this.config.QueueKeyword, StringComparison.OrdinalIgnoreCase)) return;
        if (sender == null) return;
        var queueName = BuildQueueName(sender);
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
    private static string BuildQueueName(SeString sender)
    {
        foreach (var payload in sender.Payloads)
        {
            if (payload is PlayerPayload pp && !string.IsNullOrEmpty(pp.PlayerName))
            {
                var world = pp.World.Value.Name.ToString();
                if (!string.IsNullOrEmpty(world))
                    return $"{pp.PlayerName}@{world}";
                var playerObj = MiniGamesEmporium.ObjectTable
                    .OfType<IPlayerCharacter>()
                    .FirstOrDefault(p => p.Name.TextValue.Equals(pp.PlayerName, StringComparison.OrdinalIgnoreCase));
                if (playerObj != null)
                {
                    var objWorld = playerObj.HomeWorld.Value.Name.ToString();
                    if (!string.IsNullOrEmpty(objWorld))
                        return $"{pp.PlayerName}@{objWorld}";
                }
                return pp.PlayerName;
            }
        }
        return sender.TextValue;
    }
}
