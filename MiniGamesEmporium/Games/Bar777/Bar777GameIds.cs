using System;

/// <summary>Defines the display name and placeholder constants for BAR 777, and provides helper methods for identifying walk-in and waiting placeholders.</summary>

namespace MiniGamesEmporium.Games.Bar777;
public static class Bar777GameIds
{
    public const string DisplayName = "BAR 777";
    public const string WalkInPlayerPlaceholder = "Walk-in";
    public const string WaitingPlayerPlaceholder = "Waiting...";
    public static bool Matches(string? persistedName) =>
        string.Equals(persistedName?.Trim(), DisplayName, StringComparison.OrdinalIgnoreCase);
    public static bool IsWaitingPlaceholder(string? playerName) =>
        string.Equals(playerName?.Trim(), WaitingPlayerPlaceholder, StringComparison.OrdinalIgnoreCase);
    public static bool IsAnyPlaceholder(string? playerName) =>
        IsWaitingPlaceholder(playerName) ||
        string.Equals(playerName?.Trim(), WalkInPlayerPlaceholder, StringComparison.OrdinalIgnoreCase);
}
