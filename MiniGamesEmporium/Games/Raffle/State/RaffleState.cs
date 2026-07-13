using System;
using System.Collections.Generic;
using System.Linq;
using MiniGamesEmporium.Games.Raffle.Models;

/// <summary>Serialisable snapshot of a live Raffle session, its ticket pool and the draw outcome.</summary>

namespace MiniGamesEmporium.Games.Raffle.State;
[Serializable]
public class RaffleState
{
    public string GameName { get; set; } = "Raffle";
    public long TicketCostAtStart { get; set; } = 0L;
    public long BoostedPotAtStart { get; set; } = 0L;
    public int MaxTicketsPerPlayerAtStart { get; set; } = 999;
    public int TradesToPotPercentAtStart { get; set; } = 100;
    public long PotAdjustment { get; set; } = 0L;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public int CloseHour { get; set; } = -1;
    public int CloseMinute { get; set; } = 0;
    public DateTime? CloseAtUtc { get; set; } = null;

    public List<RaffleEntry> Entries { get; set; } = new();
    public List<TicketBlock> Blocks { get; set; } = new();

    public bool IsDrawArmed { get; set; } = false;
    public bool HasDrawn { get; set; } = false;
    public int? WinningNumber { get; set; } = null;
    public string? WinnerName { get; set; } = null;
    public long WinnerPayoutGil { get; set; } = 0L;
    public Guid? PayoutTransactionId { get; set; } = null;

    public int HighestNumber => Blocks.Count == 0 ? 0 : Blocks.Max(b => b.EndNumber);

    public bool HasCloseTime => CloseHour >= 0;
}
