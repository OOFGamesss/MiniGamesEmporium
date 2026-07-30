using System;
using System.Collections.Generic;

/// <summary>One recorded vote cast by a player for a single option keyword.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Models;
[Serializable]
public class VoteRecord
{
    public string PlayerName { get; set; } = string.Empty;
    public string OptionKeyword { get; set; } = string.Empty;
    public DateTime CastAtUtc { get; set; } = DateTime.UtcNow;
}
