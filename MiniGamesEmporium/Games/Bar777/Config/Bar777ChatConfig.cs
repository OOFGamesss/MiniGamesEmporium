using System;

/// <summary>Serialisable configuration for BAR 777 chat, storing message templates for each automation trigger and the boolean toggles that control whether each message fires automatically.</summary>

namespace MiniGamesEmporium.Games.Bar777.Config;
[Serializable]
public class Bar777ChatConfig
{
    public string StartRollsMessage { get; set; } =
        "/say {player}, you may now begin your {boughtrolls} rolls!";
    public string TellAmountRequestMessage { get; set; } =
        "/tell {player} Please trade, it is {cost} gil per roll, you can buy max {rolls} rolls.";
    public string HalfwayMessage { get; set; } =
        "/say {player} You have {remaining} rolls left!";
    public bool AutoSendHalfway { get; set; } = false;
    public string UnluckyMessage { get; set; } =
        "/say Unlucky! Better luck next time! {totalpot} Gil is the new pot!";
    public bool AutoSendUnlucky { get; set; } = false;
    public string WinShoutMessage { get; set; } =
        "/shout Congratulations {player}! You just won {totalpot} Gil!";
    public bool AutoSendWinShout { get; set; } = false;
    public bool AutoStartRolls { get; set; } = false;
    public string YellPotMessage { get; set; } =
        "/yell Current Pot: {totalpot} Gil!";
    public string JoinQueueMessage { get; set; } =
        "/tell {player} You are #{position} in the queue!";
    public bool AutoSendJoinQueue { get; set; } = false;
    public string ReminderToPlayMessage { get; set; } =
        "/tell {player} Please come down to the bar and be ready to play!";
    public bool AutoSendReminderToPlay { get; set; } = false;
    public int ReminderQueueThreshold { get; set; } = 5;
    public string AnnounceKeywordMessage { get; set; } =
        "/yell Shout {keyword} to join the BAR 777 Queue!";
}
