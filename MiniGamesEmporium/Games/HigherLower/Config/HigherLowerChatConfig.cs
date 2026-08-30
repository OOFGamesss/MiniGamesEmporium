using System;

/// <summary>Serialisable chat configuration for Higher/Lower messages and toggles.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Config;
[Serializable]
public class HigherLowerChatConfig
{
    public string TellAmountRequestMessage { get; set; } =
        "/p {player} please trade {cost} gil to enter Higher/Lower!";
    public string RequestGilBuyerMessage { get; set; } =
        "/tell {buyername} Please trade {cost} gil to enter Higher/Lower for {player}!";
    public string WinShoutMessage { get; set; } =
        "/shout Congratulations {player}! You survived {rounds} rounds and won {winningamount} Gil!";
    public bool AutoSendLoss { get; set; } = false;
    public string LossUnluckyMessage { get; set; } =
        "/p Unlucky {player}! You made it to {rounds} rounds. You needed {highestround} to take the lead!";
    public string LossWinningMessage { get; set; } =
        "/p Well done {player}! You are now currently winning on {rounds} rounds!";
    public string LetsPlayMessage { get; set; } =
        "/p {player} Let's play!";
    public bool AutoSendLetsPlay { get; set; } = false;
    public string AskGuessMessage { get; set; } =
        "/p Higher or Lower than {rollednumber}?";
    public bool AutoSendAskGuess { get; set; } = false;
    public string AnnouncePotMessage { get; set; } =
        "/yell Current Pot: {totalpot} Gil!";
    public string AdvertiseMessage { get; set; } =
        "/shout Higher or Lower is running! {cost} gil to enter, guess whether the next roll is higher or lower and build your streak. The longest run takes the {totalpot} gil pot!";
    public string RulesMessage { get; set; } =
        "/party ==========  HOW TO PLAY  ==========\n" +
        "/party  {cost} gil to enter Higher/Lower!\n" +
        "/party  I roll a dice - guess if the next roll is higher or lower to build a round streak!\n" +
        "/party  Most rounds wins the pot. Current pot: {totalpot} gil!";
}
