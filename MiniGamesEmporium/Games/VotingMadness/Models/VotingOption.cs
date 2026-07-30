using System;

/// <summary>A single voting option keyword with its display colour for the live bar chart.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Models;
[Serializable]
public class VotingOption
{
    public string Keyword { get; set; } = string.Empty;
    public float ColourR { get; set; }
    public float ColourG { get; set; }
    public float ColourB { get; set; }
    public float ColourA { get; set; } = 1f;
}
