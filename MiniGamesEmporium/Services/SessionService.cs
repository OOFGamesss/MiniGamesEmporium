using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Tracks which game session is running so the host can only run one game at a time.</summary>

namespace MiniGamesEmporium.Services;
public sealed class SessionService
{
    private readonly List<GameRegistration> games = new();
    private string? pausedGameName;

    public void RegisterGame(string displayName, Func<bool> isActive, Action cancel) =>
        this.games.Add(new GameRegistration(displayName, isActive, cancel));

    public string? GetActiveGameName() =>
        this.games.FirstOrDefault(g => g.IsActive())?.DisplayName;

    public bool IsPaused
    {
        get
        {
            var active = GetActiveGameName();
            if (active != null && string.Equals(active, this.pausedGameName, StringComparison.OrdinalIgnoreCase))
                return true;
            this.pausedGameName = null;
            return false;
        }
    }

    public void PauseActiveGame() => this.pausedGameName = GetActiveGameName();

    public void ResumeActiveGame() => this.pausedGameName = null;

    public string? GetBlockingGameName(string ownGameName)
    {
        var active = GetActiveGameName();
        return active != null && !active.Equals(ownGameName, StringComparison.OrdinalIgnoreCase)
            ? active : null;
    }

    public void CancelActiveGame()
    {
        this.games.FirstOrDefault(g => g.IsActive())?.Cancel();
        this.pausedGameName = null;
    }

    private sealed record GameRegistration(string DisplayName, Func<bool> IsActive, Action Cancel);
}
