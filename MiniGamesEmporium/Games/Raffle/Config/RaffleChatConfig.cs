using System;

/// <summary>Serialisable chat configuration for Raffle message templates and auto-send toggles.</summary>

namespace MiniGamesEmporium.Games.Raffle.Config;
[Serializable]
public class RaffleChatConfig
{
    public string AdvertiseMessage { get; set; } =
        "/shout Raffle is LIVE! Tickets are {ticketcost} gil each, up to {maxtickets} per person. The pot is already {totalpot} gil and the draw is at {closetime} ST. Say {keyword} to enter!";
    public string AnnouncePotMessage { get; set; } =
        "/yell Current Raffle Pot: {totalpot} Gil!";
    public string AnnounceTicketsSoldMessage { get; set; } =
        "/yell {ticketssold} raffle tickets sold so far! Tickets are {ticketcost} Gil each, buy up to {maxtickets} tickets per person!";
    public string AnnounceClosingTimeMessage { get; set; } =
        "/yell The raffle closes at {closetime} ST ({timeleft} left)! Get your tickets in before the draw!";
    public string AnnounceJoinReminderMessage { get; set; } =
        "/yell Raffle is open! Tickets {ticketcost} Gil each (max {maxtickets} tickets per person). Say {keyword} to enter!";
    public string AnnounceRaffleClosedMessage { get; set; } =
        "/yell The raffle is now CLOSED! {ticketssold} tickets sold - pot is {totalpot} Gil! Prepare for the draw!";
    public string AnnounceWinnerMessage { get; set; } =
        "/shout Ticket {winningnumber} wins the raffle! Congratulations {winner}, you take {totalpot} Gil!";
    public bool AutoAnnounceWinner { get; set; } = false;
    public string RequestGilMessage { get; set; } =
        "/tell {player} Please trade {ticketcost} Gil per ticket to enter the raffle! You can buy up to {maxtickets} tickets!";
    public string RequestGilBuyerMessage { get; set; } =
        "/tell {buyername} Please trade Gil to buy raffle tickets for {player}! Tickets are {ticketcost} Gil each, max {maxtickets} tickets per person!";
    public string RulesMessage { get; set; } =
        "/party ==========  HOW TO PLAY  ==========\n" +
        "/party  Raffle tickets are {ticketcost} gil each, up to {maxtickets} per person.\n" +
        "/party  Say {keyword} in chat to enter, then trade the gil to claim your tickets.\n" +
        "/party  The draw is at {closetime} ST and one ticket is pulled at random.\n" +
        "/party  Current pot: {totalpot} gil!";
    public string TicketNumbersMessage { get; set; } =
        "/tell {player} Your raffle ticket number(s): {numbers}. Good luck in the draw!";
    public bool AutoSendTicketNumbers { get; set; } = false;
}
