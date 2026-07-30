using System.Linq;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Utility;

/// <summary>Formats Voting Madness chat templates with live session placeholders.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Actions;
public static class VotingMadnessMessageFormatter
{
    public static string Format(string template, PluginConfiguration config, VotingMadnessService service)
    {
        var state = service.GetState();
        var (winners, votes, percent, isTie) = service.GetResult();
        var winnerText = winners.Count == 0
            ? "None"
            : string.Join(", ", winners);
        var closeLabel = state is { HasCloseTime: true }
            ? ServerTimeUtil.FormatCloseLabel(state.CloseHour, state.CloseMinute)
            : "N/A";
        var timeLeft = state is { HasCloseTime: true }
            ? ServerTimeUtil.FormatTimeLeft(state.CloseAtUtc)
            : "N/A";

        return template
            .Replace("{options}",   service.FormatOptionsList())
            .Replace("{standings}", service.FormatStandings())
            .Replace("{winner}",    winnerText)
            .Replace("{votes}",     votes.ToString("N0"))
            .Replace("{percent}",   percent.ToString("0"))
            .Replace("{totalvotes}", service.ComputeTotalVotes().ToString("N0"))
            .Replace("{voters}",    service.ComputeUniqueVoters().ToString("N0"))
            .Replace("{closetime}", closeLabel)
            .Replace("{timeleft}",  timeLeft);
    }
}
