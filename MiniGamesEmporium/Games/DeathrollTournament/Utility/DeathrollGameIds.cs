using System;

/// <summary>Defines the display name and placeholder constants for Deathroll Tournament, and provides helpers to identify the game and distinguish real players from BYE slots.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Utility;
public static class DeathrollGameIds
{
    public const string DisplayName = "Deathroll Tournament";
    public const string ByeSlot = "BYE";

    public static bool Matches(string? persistedName) =>
        string.Equals(persistedName?.Trim(), DisplayName, StringComparison.OrdinalIgnoreCase);

    public static bool IsBye(string? slot) =>
        string.Equals(slot?.Trim(), ByeSlot, StringComparison.OrdinalIgnoreCase);
}
