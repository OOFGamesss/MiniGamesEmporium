using System;

/// <summary>Display name and placeholder constant for Coin Collector, with helpers.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Utility;
public static class CoinCollectorGameIds
{
    public const string DisplayName                 = "Coin Collector";
    public const string NoPlayerSelectedPlaceholder = "CC-NoPlayer";
    public static bool Matches(string? persistedName) =>
        string.Equals(persistedName?.Trim(), DisplayName, StringComparison.OrdinalIgnoreCase);
    public static bool IsNoPlayerPlaceholder(string? playerName) =>
        string.Equals(playerName?.Trim(), NoPlayerSelectedPlaceholder, StringComparison.OrdinalIgnoreCase);
    public static bool IsAnyPlaceholder(string? playerName) =>
        IsNoPlayerPlaceholder(playerName);
}
