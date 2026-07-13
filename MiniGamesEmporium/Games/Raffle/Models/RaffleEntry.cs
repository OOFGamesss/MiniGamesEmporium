using System;

/// <summary>A registered raffle player, tracking overpay credit and their buyer.</summary>

namespace MiniGamesEmporium.Games.Raffle.Models;
[Serializable]
public class RaffleEntry
{
    public string PlayerName { get; set; } = string.Empty;
    public bool Verified { get; set; } = true;
    public long CreditGil { get; set; } = 0L;
    public string Buyer { get; set; } = string.Empty;
}
