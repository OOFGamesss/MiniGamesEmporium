using System;

/// <summary>Serialisable chat configuration for Deathroll Tournament messages and toggles.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Config;
[Serializable]
public class DeathrollTournamentChatConfig
{
    public int ConfigVersion { get; set; } = 1;

    public bool UseCustomTurnReminderMessage { get; set; } = false;
    public string TurnReminderMessage { get; set; } =
        "/tell {player} it's your turn, please stop erping!";
    public string RequestGilMessage { get; set; } =
        "/tell {player} Please pay {entrycost} Gil to enter the Deathroll Tournament!";
    public string RequestGilBuyerMessage { get; set; } =
        "/tell {buyername} Please pay {entrycost} Gil to enter the Deathroll Tournament for {player}!";
    public string AnnounceBracketMessage { get; set; } =
        "/yell It's time for {round}. Here are the contenders:";
    public string AnnounceMatchupMessage { get; set; } =
        "/yell [{round}] {player1} vs {player2} - roll /random 10 to decide who goes first!";
    public bool AutoAnnounceMatchup { get; set; } = false;
    public string AnnouncePrizeMessage { get; set; } =
        "/yell Deathroll Tournament Prize: {prize}!";
    public bool AutoAnnouncePrize { get; set; } = false;
    public string AnnounceTournamentWinnerMessage { get; set; } =
        "/shout {winner} wins the Deathroll Tournament and takes {prize}! Congratulations!";
    public bool AutoAnnounceWinner { get; set; } = false;
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
    public string AnnounceBettingOpenMessage { get; set; } =
        "/yell Betting is now open! Bet {betunit} Gil on who will win the tournament - Send me a tell with \"{betkeyword} <name>\" then trade the gil!";
    public bool AutoAnnounceBettingOpen { get; set; } = false;
    public string AnnounceBettingPotMessage { get; set; } =
        "/yell Betting Pot: {bettingpot} Gil!";
    public string RequestBetGilMessage { get; set; } =
        "/tell {player} Please pay {betunit} Gil to confirm your bet on {bettarget}!";
    public string AnnounceBetWinnerMessage { get; set; } =
        "/shout {betwinners} correctly bet on {winner} and takes the entire {bettingpot} Gil betting pot!";
    public string AnnounceBetWinnersMessage { get; set; } =
        "/shout {betwinners} correctly bet on {winner} and share the {bettingpot} Gil betting pot!";
    public bool AutoAnnounceBetWinners { get; set; } = false;
}
