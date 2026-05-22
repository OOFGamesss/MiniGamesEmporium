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

/// <summary>Subscribes to the Dalamud chat message event to detect BAR 777 rolls, Deathroll Tournament rolls, handle Gil trade messages, and enqueue players who post the configured queue join keyword.</summary>

namespace MiniGamesEmporium.Services;
public sealed class ChatListener : IDisposable
{
    private readonly IChatGui chatGui;
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly IPluginLog log;
    private readonly TradeHandler tradeHandler;
    public ChatListener(IChatGui chatGui, PluginConfiguration config, SessionService sessionService, DeathrollTournamentService deathrollService, IPluginLog log, IObjectTable objectTable)
    {
        this.chatGui          = chatGui;
        this.config           = config;
        this.sessionService   = sessionService;
        this.deathrollService = deathrollService;
        this.log              = log;
        this.tradeHandler     = new TradeHandler(config, sessionService, deathrollService, objectTable, log);
        this.chatGui.ChatMessage += OnChatMessage;
    }
    public void Dispose()
    {
        this.chatGui.ChatMessage -= OnChatMessage;
    }
    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (this.sessionService.IsPaused) return;
        var senderText = message.Sender?.TextValue ?? string.Empty;
        var messageText = message.Message?.TextValue ?? string.Empty;
        var kind = message.LogKind;
        if (string.IsNullOrEmpty(messageText)) return;
        this.log.Debug($"[MGE] [{kind}({(ushort)kind})] '{messageText}'");
        this.tradeHandler.Process(messageText);
        if (kind == XivChatType.RandomNumber)
        {
            TryHandleBar777Roll(messageText);
            TryHandleDeathrollRoll(messageText);
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
            XivChatType.Say         => listen.Say,
            XivChatType.Shout       => listen.Shout,
            XivChatType.Yell        => listen.Yell,
            XivChatType.TellIncoming => listen.TellIncoming,
            _ => false,
        };
    }
    private void TryHandleBar777Roll(string message)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName) || !session.PaymentVerified) return;
        var prefix = $"Random! {session.PlayerName}";
        if (!message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;
        if (message.Contains("(out of ", StringComparison.OrdinalIgnoreCase)) return;
        if (!TryParseRollValue(message, out var rollValue)) return;
        this.sessionService.RecordRoll(rollValue);
        this.log.Information($"Roll recorded: {rollValue} for {session.PlayerName}");
    }
    private void TryHandleDeathrollRoll(string message)
    {
        if (!this.deathrollService.HasActiveTournament()) return;
        if (!TryParseDeathrollMessage(message, out var playerName, out var rollValue, out var rollMax)) return;
        if (playerName.Equals("You", StringComparison.OrdinalIgnoreCase))
        {
            var localName = MiniGamesEmporium.ObjectTable.LocalPlayer?.Name.TextValue;
            if (!string.IsNullOrEmpty(localName))
                playerName = localName;
        }
        if (!this.deathrollService.TryCatchNextMatchOrderRoll(playerName, rollValue, rollMax) &&
            !this.deathrollService.TryCatchNextGameOrderRoll(playerName, rollValue, rollMax))
            this.deathrollService.TryRecordRoll(playerName, rollValue, rollMax);
    }
    private static bool TryParseRollValue(string message, out int rollValue)
    {
        rollValue = 0;
        const string marker = "rolls a ";
        var idx = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        var start = idx + marker.Length;
        var rest = message.Substring(start);
        var end = 0;
        while (end < rest.Length && char.IsDigit(rest[end])) end++;
        if (end == 0) return false;
        return int.TryParse(rest.Substring(0, end), out rollValue);
    }
    private static bool TryParseDeathrollMessage(string message, out string playerName, out int rollValue, out int rollMax)
    {
        playerName = string.Empty;
        rollValue  = 0;
        rollMax    = 0;
        const string prefix    = "Random! ";
        const string outOfMark = " (out of ";
        if (!message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var body    = message.Substring(prefix.Length);
        var rollIdx = body.IndexOf(" rolls a ", StringComparison.OrdinalIgnoreCase);
        var rollMarkLen = 9;
        if (rollIdx < 0)
        {
            rollIdx = body.IndexOf(" roll a ", StringComparison.OrdinalIgnoreCase);
            rollMarkLen = 8;
        }
        if (rollIdx < 0) return false;
        playerName = body.Substring(0, rollIdx);
        var afterRolls = body.Substring(rollIdx + rollMarkLen);
        var numLen = 0;
        while (numLen < afterRolls.Length && char.IsDigit(afterRolls[numLen])) numLen++;
        if (numLen == 0) return false;
        if (!int.TryParse(afterRolls.Substring(0, numLen), out rollValue)) return false;
        var rest   = afterRolls.Substring(numLen);
        var outIdx = rest.IndexOf(outOfMark, StringComparison.OrdinalIgnoreCase);
        if (outIdx >= 0)
        {
            var afterOut = rest.Substring(outIdx + outOfMark.Length);
            var maxLen   = 0;
            while (maxLen < afterOut.Length && char.IsDigit(afterOut[maxLen])) maxLen++;
            if (maxLen > 0) int.TryParse(afterOut.Substring(0, maxLen), out rollMax);
        }
        return true;
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
