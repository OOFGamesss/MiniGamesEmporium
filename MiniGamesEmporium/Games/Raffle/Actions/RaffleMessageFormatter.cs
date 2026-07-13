using System.Linq;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.Services;

/// <summary>Fills Raffle message templates with live session values.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class RaffleMessageFormatter
{
    public static string Format(
        string template,
        PluginConfiguration config,
        string winner = "",
        int winningNumber = 0,
        long? totalPotOverride = null,
        string player = "",
        string buyerName = "",
        string numbers = "")
    {
        var state      = config.RaffleSession;
        var cfg        = config.Raffle;
        var ticketCost = state?.TicketCostAtStart ?? cfg.TicketCost;
        var boostedPot = state?.BoostedPotAtStart ?? cfg.BoostedPot;
        var maxTickets = state?.MaxTicketsPerPlayerAtStart ?? cfg.MaxTicketsPerPlayer;
        var ticketsSold = state?.Blocks.Sum(b => b.Count) ?? 0;
        var pot        = totalPotOverride ?? ComputeTotalPot(config);
        var closeLabel = state != null && state.HasCloseTime
            ? RaffleTimeUtil.FormatCloseLabel(state.CloseHour, state.CloseMinute)
            : "TBA";
        var timeLeft   = state != null && state.HasCloseTime
            ? RaffleTimeUtil.FormatTimeLeft(state.CloseAtUtc)
            : "TBA";

        return template
            .Replace("{ticketcost}",    ticketCost.ToString("N0"))
            .Replace("{totalpot}",      pot.ToString("N0"))
            .Replace("{boostedpot}",    boostedPot.ToString("N0"))
            .Replace("{ticketssold}",   ticketsSold.ToString())
            .Replace("{maxtickets}",    maxTickets.ToString())
            .Replace("{closetime}",     closeLabel)
            .Replace("{timeleft}",      timeLeft)
            .Replace("{winner}",        PlayerInfoService.StripWorld(winner))
            .Replace("{winningnumber}", winningNumber.ToString())
            .Replace("{keyword}",       cfg.JoinKeyword)
            .Replace("{player}",        player.Trim())
            .Replace("{buyername}",     buyerName.Trim())
            .Replace("{numbers}",       numbers.Trim());
    }

    public static long ComputeTotalPot(PluginConfiguration config)
    {
        var state = config.RaffleSession;
        if (state == null) return config.Raffle.BoostedPot;
        var ticketsSold = state.Blocks.Sum(b => b.Count);
        var tradesToPot = state.TicketCostAtStart * ticketsSold * state.TradesToPotPercentAtStart / 100;
        return tradesToPot + state.BoostedPotAtStart + state.PotAdjustment;
    }
}
