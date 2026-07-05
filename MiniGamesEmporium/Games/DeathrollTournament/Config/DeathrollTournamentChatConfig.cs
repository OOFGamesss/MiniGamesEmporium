using System;

/// <summary>Serialisable chat configuration for Deathroll Tournament messages and toggles.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Config;
[Serializable]
public class DeathrollTournamentChatConfig
{
    public string AnnounceBracketMessage { get; set; } =
        "/yell It's time for {round}. Here are the contenders:";
    public string AnnounceMatchupMessage { get; set; } =
        "/yell [{round}] {player1} vs {player2} - roll /random 10 to decide who goes first!";
    public bool AutoAnnounceMatchup { get; set; } = false;
    public string AnnounceTournamentWinnerMessage { get; set; } =
        "/shout {winner} wins the Deathroll Tournament and takes {totalpot} Gil! Congratulations!";
    public bool AutoAnnounceWinner { get; set; } = false;
    public string AnnouncePotMessage { get; set; } =
        "/yell Deathroll Tournament Pot: {totalpot} Gil! ({playercount} players x {entrycost} Gil + {boostedpot} Gil boosted)";
    public string RequestGilMessage { get; set; } =
        "/tell {player} Please pay {entrycost} Gil to enter the Deathroll Tournament!";
    public string RequestGilBuyerMessage { get; set; } =
        "/tell {buyername} Please pay {entrycost} Gil to enter the Deathroll Tournament for {player}!";
    public string RerollRandom10Message { get; set; } =
        "/yell Uh-oh! You both rolled a {random10}! Please roll /random 10 again!";
    public bool AutoAnnounceRerollRandom10 { get; set; } = false;
    public string AnnounceFirstPlayerMessage { get; set; } =
        "/yell {firstplayer} it's your time to shine! Roll /random";
    public bool AutoAnnounceFirstPlayer { get; set; } = false;
    public string AnnounceRoundWinMessage { get; set; } =
        "/yell {roundwinner} wins that round! • Current score: {roundscore}. • {roundsleft} round(s) left to go! • Roll /random 10 to start the next round!";
    public bool AutoAnnounceRoundWin { get; set; } = false;
    public string AnnounceMatchWinMessage { get; set; } =
        "/yell {matchwinner} has taken {matchloser} out of the cup! Finishing score: {roundscore}";
    public bool AutoAnnounceMatchWin { get; set; } = false;
}
