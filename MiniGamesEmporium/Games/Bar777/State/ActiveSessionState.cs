using System;
using System.Collections.Generic;

/// <summary>Serialisable snapshot of the currently active game session, tracking the player name, world, roll log, payment status, win detection flag, and session start time.</summary>

namespace MiniGamesEmporium.Games.Bar777.State;
[Serializable]
public class ActiveSessionState
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
}
