using System;

/// <summary>Serialisable chat configuration for Coin Collector messages and toggles.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Config;
[Serializable]
public class CoinCollectorChatConfig
{
    public string TellAmountRequestMessage { get; set; } =
        "/p {player} please trade {cost} gil to enter Coin Collector!";
    public string RequestGilBuyerMessage { get; set; } =
        "/tell {buyername} Please trade {cost} Gil to enter Coin Collector for {player}!";
    public string WinShoutMessage { get; set; } =
        "/shout Congratulations {player}! You collected {coins} coins and won {winningamount} Gil!";
    public bool AutoSendLoss { get; set; } = false;
    public string LossUnluckyMessage { get; set; } =
        "/p Unlucky {player}! You collected {coins} coins. You needed {highestcoins} to take the lead!";
    public string LossWinningMessage { get; set; } =
        "/p Well done {player}! You are now currently winning on {coins} coins!";
    public string AskRollMessage { get; set; } =
        "/p {player} roll /dice {rollmax} to collect a coin!";
    public string AskRollWithCoinsMessage { get; set; } =
        "/p {player} you have {coins} coins - roll /dice {rollmax} to collect another!";
    public bool AutoSendAskRoll { get; set; } = false;
    public string AnnouncePotMessage { get; set; } =
        "/yell Current Pot: {totalpot} Gil!";
    public string AdvertiseMessage { get; set; } =
        "/shout Coin Collector is running! {cost} gil to enter, keep rolling without hitting a 1 and collect a coin every time. The biggest hoard takes the {totalpot} gil pot!";
    public string RulesMessage { get; set; } =
        "/party ==========  HOW TO PLAY  ==========\n" +
        "/party  {cost} gil to enter Coin Collector!\n" +
        "/party  Roll /dice to get your starting number, then roll /dice using your last result as the new maximum.\n" +
        "/party  Every roll that is not a 1 earns you a coin. Roll a 1 and your turn is over!\n" +
        "/party  Most coins wins the pot. Current pot: {totalpot} gil!";
}
