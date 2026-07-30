using System;

/// <summary>Serialisable chat templates for Voting Madness announcements.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Config;
[Serializable]
public class VotingMadnessChatConfig
{
    public string AnnounceOptionsMessage { get; set; } =
        "/yell Vote now! Options: {options}. Say your choice in chat!";
    public string VoteStartedMessage { get; set; } =
        "/yell Voting has started! Cast your vote by saying one of: {options}";
    public string AnnounceClosingTimeMessage { get; set; } =
        "/yell Voting closes at {closetime} ST ({timeleft} left)! Get your votes in!";
    public string VoteEndedMessage { get; set; } =
        "/yell Voting is now CLOSED! {totalvotes} votes from {voters} players.";
    public string AnnounceWinningVoteMessage { get; set; } =
        "/yell The winning vote is {winner} with {percent}% ({votes} of {totalvotes} votes)!";
    public string AnnounceStandingsMessage { get; set; } =
        "/yell Current standings: {standings}";
    public string AnnounceTieMessage { get; set; } =
        "/yell It's a tie between {winner}! Each has {percent}% ({votes} of {totalvotes} votes).";
}
