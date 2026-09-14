using System;
using MiniGamesEmporium.Utility;

/// <summary>Serialisable chat configuration for BAR 777 messages and toggles.</summary>

namespace MiniGamesEmporium.Games.Bar777.Config;

public enum ChatChannel
{
    Say,
    Party,
    Alliance,
}

[Serializable]
public class Bar777ChatConfig
{
    public ChatChannel Channel { get; set; } = ChatChannel.Say;

    public string StartRollsMessage { get; set; } =
        "/say {player}, you may now begin your {boughtrolls} rolls!";
    public string TellAmountRequestMessage { get; set; } =
        "/tell {player} Please trade, it is {cost} gil per roll, you can buy max {rolls} rolls.";
    public string TellBuyerRequestMessage { get; set; } =
        "/tell {buyername} Please trade, it is {cost} gil per roll, you can buy max {rolls} rolls for {player}.";
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
    public string NextPlayerUpMessage { get; set; } =
        "/say Next player up is {player}";
    public string AdvertiseMessage { get; set; } =
        "/shout BAR 777 is OPEN! {cost} gil per roll, up to {rolls} rolls each. Match the winning number and take the whole {totalpot} gil pot! Shout {keyword} to join the queue!";
    public string RulesMessage { get; set; } =
        "/s ==========  HOW TO PLAY  ==========\n" +
        "/s  {cost} gil per roll, buy up to {rolls} rolls (max {maxcost} gil).\n" +
        "/s  Type /random each roll - match the winning number to win the whole pot!\n" +
        "/s  Current pot: {totalpot} gil. Every losing roll grows the pot!";

    public void ApplyChatChannel(ChatChannel channel)
    {
        this.Channel = channel;
        string target = channel switch
        {
            ChatChannel.Party => "/p",
            ChatChannel.Alliance => "/a",
            _ => "/s",
        };
        this.StartRollsMessage = ChatChannelText.ConvertLeadingChannel(this.StartRollsMessage, target);
        this.TellAmountRequestMessage = ChatChannelText.ConvertLeadingChannel(this.TellAmountRequestMessage, target);
        this.TellBuyerRequestMessage = ChatChannelText.ConvertLeadingChannel(this.TellBuyerRequestMessage, target);
        this.HalfwayMessage = ChatChannelText.ConvertLeadingChannel(this.HalfwayMessage, target);
        this.UnluckyMessage = ChatChannelText.ConvertLeadingChannel(this.UnluckyMessage, target);
        this.WinShoutMessage = ChatChannelText.ConvertLeadingChannel(this.WinShoutMessage, target);
        this.YellPotMessage = ChatChannelText.ConvertLeadingChannel(this.YellPotMessage, target);
        this.JoinQueueMessage = ChatChannelText.ConvertLeadingChannel(this.JoinQueueMessage, target);
        this.ReminderToPlayMessage = ChatChannelText.ConvertLeadingChannel(this.ReminderToPlayMessage, target);
        this.AnnounceKeywordMessage = ChatChannelText.ConvertLeadingChannel(this.AnnounceKeywordMessage, target);
        this.NextPlayerUpMessage = ChatChannelText.ConvertLeadingChannel(this.NextPlayerUpMessage, target);
        this.AdvertiseMessage = ChatChannelText.ConvertLeadingChannel(this.AdvertiseMessage, target);
        this.RulesMessage = ChatChannelText.ConvertLeadingChannelPerLine(this.RulesMessage, target);
    }
}
