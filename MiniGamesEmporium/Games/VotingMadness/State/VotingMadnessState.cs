using System;
using System.Collections.Generic;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Models;

/// <summary>Serialisable snapshot of a live Voting Madness session and its tallies.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.State;
[Serializable]
public class VotingMadnessState
{
    public string GameName { get; set; } = "Voting Madness";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public List<VotingOption> Options { get; set; } = new();
    public QueueConfig VoteChannels { get; set; } = new();
    public bool MultipleChoice { get; set; }
    public bool AllowMultipleVotes { get; set; }
    public int CloseHour { get; set; } = -1;
    public int CloseMinute { get; set; }
    public DateTime? CloseAtUtc { get; set; }
    public bool IsVotingClosed { get; set; }
    public List<VoteRecord> Votes { get; set; } = new();

    public bool HasCloseTime => CloseHour >= 0;
}
