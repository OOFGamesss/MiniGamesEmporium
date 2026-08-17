using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Tracks which game sessions are running and which one holds chat focus. Any number of games may be live, but only the focused game receives chat and incoming trades.</summary>

namespace MiniGamesEmporium.Services;
public sealed class SessionService
{
    private readonly List<GameRegistration> games = new();
    private readonly HashSet<string> knownActive = new(StringComparer.OrdinalIgnoreCase);
    private string? focusedGameName;

    public void RegisterGame(string displayName, Func<bool> isActive) =>
        this.games.Add(new GameRegistration(displayName, isActive));

    public string? FocusedGameName
    {
        get
        {
            Reconcile();
            return this.focusedGameName;
        }
    }

    public bool IsFocused(string gameName)
    {
        Reconcile();
        return this.focusedGameName != null
            && this.focusedGameName.Equals(gameName, StringComparison.OrdinalIgnoreCase);
    }

    public void Focus(string gameName)
    {
        Reconcile();
        var game = this.games.FirstOrDefault(g => g.DisplayName.Equals(gameName, StringComparison.OrdinalIgnoreCase));
        if (game == null || !game.IsActive()) return;
        this.focusedGameName = game.DisplayName;
    }

    public void ClearFocus()
    {
        Reconcile();
        this.focusedGameName = null;
    }

    private void Reconcile()
    {
        foreach (var game in this.games)
        {
            var isActive  = game.IsActive();
            var wasActive = this.knownActive.Contains(game.DisplayName);
            if (isActive == wasActive) continue;
            if (isActive)
            {
                this.knownActive.Add(game.DisplayName);
                this.focusedGameName = game.DisplayName;
            }
            else
            {
                this.knownActive.Remove(game.DisplayName);
                if (game.DisplayName.Equals(this.focusedGameName, StringComparison.OrdinalIgnoreCase))
                    this.focusedGameName = null;
            }
        }
    }

    private sealed record GameRegistration(string DisplayName, Func<bool> IsActive);
}
