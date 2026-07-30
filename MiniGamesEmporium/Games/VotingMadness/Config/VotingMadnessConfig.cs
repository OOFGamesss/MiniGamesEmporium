using System;
using System.Collections.Generic;
using System.Linq;
using MiniGamesEmporium.Config;
using Newtonsoft.Json;

/// <summary>Serialisable configuration and pre-session defaults for Voting Madness.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Config;
[Serializable]
public class VotingMadnessConfig
{
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<string> Options { get; set; } = new() { "Yes", "No" };
    public QueueConfig VoteChannels { get; set; } = new();
    public bool MultipleChoice { get; set; } = false;
    public bool AllowMultipleVotes { get; set; } = false;
    public int CloseHour { get; set; } = -1;
    public int CloseMinute { get; set; } = 0;
    public VotingMadnessChatConfig Chat { get; set; } = new();

    public void SanitiseOptions()
    {
        Options ??= new List<string>();
        var cleaned = new List<string>();
        foreach (var option in Options)
        {
            var trimmed = (option ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (cleaned.Any(existing => existing.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
                continue;
            cleaned.Add(trimmed);
        }

        if (!cleaned.Any(c => c.Equals("Yes", StringComparison.OrdinalIgnoreCase)) && cleaned.Count < 2)
            cleaned.Insert(0, "Yes");
        if (!cleaned.Any(c => c.Equals("No", StringComparison.OrdinalIgnoreCase)) && cleaned.Count < 2)
            cleaned.Add("No");
        while (cleaned.Count < 2)
            cleaned.Add($"Option {cleaned.Count + 1}");

        Options = cleaned;
    }
}
