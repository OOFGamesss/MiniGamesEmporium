using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using System;

/// <summary>When Auto Next Match is enabled, listens for a match completion and automatically advances to the next match after the configured delay.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Automation;
public sealed class DeathrollNextMatchAutomation : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private DateTime? pendingAdvanceAt;

    public DeathrollNextMatchAutomation(PluginConfiguration config, DeathrollTournamentService deathrollService)
    {
        this.config           = config;
        this.deathrollService = deathrollService;
        deathrollService.MatchCompleted          += OnMatchCompleted;
        deathrollService.NextMatchCatchTriggered += OnNextMatchCaught;
        MiniGamesEmporium.Framework.Update       += OnFrameworkUpdate;
    }

    private void OnMatchCompleted()
    {
        if (!this.config.DeathrollTournament.AutoNextMatch) return;
        var delay = Math.Clamp(this.config.DeathrollTournament.AutoNextMatchDelaySeconds, 0, 60);
        this.pendingAdvanceAt = DateTime.UtcNow.AddSeconds(delay);
    }

    private void OnNextMatchCaught() => this.pendingAdvanceAt = null;

    private void OnFrameworkUpdate(IFramework _)
    {
        if (this.pendingAdvanceAt == null) return;
        if (DateTime.UtcNow < this.pendingAdvanceAt.Value) return;
        this.pendingAdvanceAt = null;
        var state = this.config.DeathrollTournamentSession;
        if (state == null || state.TournamentWinner != null) return;
        if (state.ActiveMatchPhase != MatchPhase.MatchComplete) return;
        this.deathrollService.MoveToNextMatch();
        this.deathrollService.StartCurrentMatch();
    }

    public void Dispose()
    {
        MiniGamesEmporium.Framework.Update           -= OnFrameworkUpdate;
        this.deathrollService.MatchCompleted          -= OnMatchCompleted;
        this.deathrollService.NextMatchCatchTriggered -= OnNextMatchCaught;
    }
}
