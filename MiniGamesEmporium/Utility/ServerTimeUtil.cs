using System;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

/// <summary>Server time helpers for optional closing countdowns, read from the game clock.</summary>

namespace MiniGamesEmporium.Utility;
public static class ServerTimeUtil
{
    public static DateTime NowServer()
    {
        var epoch = Framework.GetServerTime();
        if (epoch > 0) return DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;
        return DateTime.UtcNow;
    }

    public static (int Hour, int Minute) SuggestCloseTime()
    {
        var t = NowServer().AddHours(1);
        if (t.Minute > 0) t = t.AddHours(1);
        return (t.Hour, 0);
    }

    public static DateTime NextOccurrence(int hour, int minute)
    {
        var h   = Math.Clamp(hour, 0, 23);
        var m   = Math.Clamp(minute, 0, 59);
        var now = NowServer();
        var candidate = new DateTime(now.Year, now.Month, now.Day, h, m, 0, DateTimeKind.Utc);
        if (candidate <= now) candidate = candidate.AddDays(1);
        return candidate;
    }

    public static string FormatTimeLeft(DateTime? closeAtUtc)
    {
        if (closeAtUtc == null) return "No close time set";
        var remaining = closeAtUtc.Value - NowServer();
        if (remaining <= TimeSpan.Zero) return "Closed";
        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays}d {remaining.Hours}h {remaining.Minutes}m";
        if (remaining.TotalHours >= 1)
            return $"{remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s";
        return $"{remaining.Minutes}m {remaining.Seconds}s";
    }

    public static string FormatCloseLabel(int hour, int minute) =>
        $"{Math.Clamp(hour, 0, 23):00}:{Math.Clamp(minute, 0, 59):00}";
}
