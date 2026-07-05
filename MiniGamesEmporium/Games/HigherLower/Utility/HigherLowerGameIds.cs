using System;

/// <summary>Display name and placeholder constant for Higher/Lower, with helpers.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Utility;
public static class HigherLowerGameIds
{
    public const string DisplayName                 = "Higher/Lower";
    public const string NoPlayerSelectedPlaceholder = "HL-NoPlayer";
    public static bool Matches(string? persistedName) =>
        string.Equals(persistedName?.Trim(), DisplayName, StringComparison.OrdinalIgnoreCase);
    public static bool IsNoPlayerPlaceholder(string? playerName) =>
        string.Equals(playerName?.Trim(), NoPlayerSelectedPlaceholder, StringComparison.OrdinalIgnoreCase);
    public static bool IsAnyPlaceholder(string? playerName) =>
        IsNoPlayerPlaceholder(playerName);
}
