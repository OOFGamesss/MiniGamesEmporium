using System;

/// <summary>Tracks a single bettor's share of the betting pot and how much has been paid out.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Models;
[Serializable]
public class DeathrollBetPayout
{
    public string BettorName { get; set; } = string.Empty;
    public long ShareGil { get; set; } = 0;
    public long PaidGil { get; set; } = 0;
    public Guid? PayoutTransactionId { get; set; } = null;
}
