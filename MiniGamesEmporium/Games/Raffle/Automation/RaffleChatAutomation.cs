using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Actions;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Services;

/// <summary>Subscribes to Raffle service events and dispatches the configured announcements to chat when each fires.</summary>

namespace MiniGamesEmporium.Games.Raffle.Automation;
public sealed class RaffleChatAutomation : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly RaffleService raffleService;
    private readonly ChatQueueService chatQueue;

    public RaffleChatAutomation(PluginConfiguration config, RaffleService raffleService, ChatQueueService chatQueue)
    {
        this.config        = config;
        this.raffleService = raffleService;
        this.chatQueue     = chatQueue;
        this.raffleService.WinnerDrawn += OnWinnerDrawn;
        this.raffleService.TicketsAutoGranted += OnTicketsAutoGranted;
    }

    private void OnWinnerDrawn(string winner, int winningNumber, long totalPot)
    {
        if (!this.config.Raffle.Chat.AutoAnnounceWinner) return;
        AnnounceWinner.Execute(this.config, this.chatQueue, winner, winningNumber, totalPot);
    }

    private void OnTicketsAutoGranted(string playerEntry)
    {
        if (!this.config.Raffle.Chat.AutoSendTicketNumbers) return;
        if (this.raffleService.AreNumbersHidden()) return;
        var numbers = this.raffleService.GetPlayerRangeLabel(playerEntry);
        TellTicketNumbers.Execute(playerEntry, numbers, this.config, this.chatQueue);
    }

    public void Dispose()
    {
        this.raffleService.WinnerDrawn -= OnWinnerDrawn;
        this.raffleService.TicketsAutoGranted -= OnTicketsAutoGranted;
    }
}
