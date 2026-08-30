using Dalamud.Interface;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>Identifies each mini game and describes how it is presented in the navigation sidebar.</summary>

namespace MiniGamesEmporium.UI.Components;
public enum MiniGame
{
    Bar777,
    DeathrollTournament,
    Raffle,
    HigherLower,
    MinefieldGambit,
    BeerPong,
    Darts,
    EightBallPool,
    RussianRoulette,
    RaidBoss,
    DealOrNoDeal,
    VotingMadness,
    CoinCollector,
}

public sealed record MiniGameNavEntry(
    MiniGame Game,
    Func<string> Label,
    FontAwesomeIcon Icon,
    Vector4 Colour,
    IReadOnlyList<GameSection> Sections);
