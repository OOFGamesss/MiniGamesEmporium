using System;
using System.Collections.Generic;

/// <summary>Serialisable snapshot of the active BAR 777 session.</summary>

namespace MiniGamesEmporium.Models;
[Serializable]
public class ActiveSession
{
    public string GameName { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerWorld { get; set; } = string.Empty;
    public bool PlayerSet { get; set; } = false;
    public int RollsUsed { get; set; } = 0;
    public int RollsAllowed { get; set; } = 20;
    public bool PaymentVerified { get; set; } = false;
    public bool WinTriggered { get; set; } = false;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public int AmountTraded { get; set; } = 0;
    public List<int> RollLog { get; set; } = [];
    public string PaidByPlayerName { get; set; } = string.Empty;
    public long WinnerPayoutGil { get; set; } = 0;
}
