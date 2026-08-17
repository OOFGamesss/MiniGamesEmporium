using System;

/// <summary>A contiguous run of raffle ticket numbers granted to a single player in purchase order.</summary>

namespace MiniGamesEmporium.Games.Raffle.Models;
[Serializable]
public class TicketBlock
{
    public string Owner { get; set; } = string.Empty;
    public int StartNumber { get; set; }
    public int Count { get; set; }

    public int EndNumber => StartNumber + Count - 1;

    public bool Contains(int number) => number >= StartNumber && number <= EndNumber;

    public string RangeLabel => Count switch
    {
        <= 1 => $"{StartNumber}",
        2    => $"{StartNumber}, {EndNumber}",
        _    => $"{StartNumber}-{EndNumber}",
    };
}
