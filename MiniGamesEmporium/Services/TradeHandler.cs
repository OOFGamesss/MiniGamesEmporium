using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>Parses system trade messages to identify the trading partner and Gil received, then forwards completed trades to the session service for payment verification.</summary>

namespace MiniGamesEmporium.Services;
public sealed class TradeHandler
{
    private static readonly Regex TradeRequestRegex  = new(@"Trade request sent to (.+?)\.",     RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TradeIncomingRegex = new(@"^(.+?) wishes to trade with you\.", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ReceiveGilRegex    = new(@"You receive ([\d,]+) gil\.",         RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TradeCompleteRegex = new(@"Trade complete\.",                   RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TradeCancelRegex   = new(@"Trade cancel",                      RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly IObjectTable objectTable;
    private readonly IPluginLog log;
    private string? partner;
    private int received;
    public TradeHandler(PluginConfiguration config, SessionService sessionService, IObjectTable objectTable, IPluginLog log)
    {
        this.config = config;
        this.sessionService = sessionService;
        this.objectTable = objectTable;
        this.log = log;
    }
    public void Process(string text)
    {
        if (CheckRequest(text) || CheckReceive(text)) return;
        if (TradeCancelRegex.IsMatch(text))
        {
            this.partner = null;
            this.received = 0;
            return;
        }
        if (TradeCompleteRegex.IsMatch(text) && this.partner != null)
            ApplyTrade();
    }
    private bool CheckRequest(string text)
    {
        var match = TradeRequestRegex.Match(text);
        if (!match.Success) match = TradeIncomingRegex.Match(text);
        if (!match.Success) return false;
        this.partner  = match.Groups[1].Value.Trim();
        this.received = 0;
        return true;
    }
    private bool CheckReceive(string text)
    {
        var match = ReceiveGilRegex.Match(text);
        if (!match.Success || this.partner == null) return false;
        if (int.TryParse(match.Groups[1].Value.Replace(",", ""), out var gil))
            this.received += gil;
        return true;
    }
    private void ApplyTrade()
    {
        if (this.partner == null) return;
        if (this.received > 0)
        {
            var world = LookupPlayerWorld(this.partner);
            this.log.Information($"Trade complete: {this.partner} gave {this.received:N0} gil.");
            this.sessionService.VerifyPayment(this.partner, this.received, world);
        }
        this.partner  = null;
        this.received = 0;
    }
    private string LookupPlayerWorld(string playerName)
    {
        var player = this.objectTable
            .OfType<IPlayerCharacter>()
            .FirstOrDefault(x => x.Name.TextValue.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        return player?.HomeWorld.Value.Name.ToString() ?? string.Empty;
    }
}
