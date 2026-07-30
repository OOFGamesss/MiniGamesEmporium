using System;
using MiniGamesEmporium.Utility;

/// <summary>Raffle-facing wrapper around the shared server time helpers.</summary>

namespace MiniGamesEmporium.Games.Raffle.Utility;
public static class RaffleTimeUtil
{
    public static DateTime NowServer() => ServerTimeUtil.NowServer();
    public static (int Hour, int Minute) SuggestCloseTime() => ServerTimeUtil.SuggestCloseTime();
    public static DateTime NextOccurrence(int hour, int minute) => ServerTimeUtil.NextOccurrence(hour, minute);
    public static string FormatTimeLeft(DateTime? closeAtUtc) => ServerTimeUtil.FormatTimeLeft(closeAtUtc);
    public static string FormatCloseLabel(int hour, int minute) => ServerTimeUtil.FormatCloseLabel(hour, minute);
}
