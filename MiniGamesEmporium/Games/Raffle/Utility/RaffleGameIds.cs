using System;

/// <summary>Display name and constants for the Raffle game, with helpers.</summary>

namespace MiniGamesEmporium.Games.Raffle.Utility;
public static class RaffleGameIds
{
    public const string DisplayName = "Raffle";
    public const int MaxTicketPoolSize = 999;

    public static bool Matches(string? persistedName) =>
        string.Equals(persistedName?.Trim(), DisplayName, StringComparison.OrdinalIgnoreCase);
}
