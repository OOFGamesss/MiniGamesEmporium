using System;
using MiniGamesEmporium.Models;

/// <summary>Writes a single combined winner payout transaction shared by all games, updating it in place as further gil is paid.</summary>

namespace MiniGamesEmporium.Services;
public static class PayoutTransactionRecorder
{
    public static Guid Record(HistoryService historyService, string gameName, string winnerName, long totalPaid, Guid? existingId)
    {
        var id = existingId ?? Guid.NewGuid();
        historyService.UpsertTransaction(new TransactionRecord
        {
            TransactionId = id,
            PlayerName    = $"Winner Payout: {PlayerInfoService.StripWorld(winnerName)}",
            Amount        = (int)Math.Max(-totalPaid, int.MinValue),
            Timestamp     = DateTime.UtcNow,
            GameName      = gameName,
        });
        return id;
    }
}
