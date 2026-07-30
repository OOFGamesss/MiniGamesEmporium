using Dalamud.Interface;
using System;

/// <summary>The sections a game panel can expose in the sidebar, with their labels and icons.</summary>

namespace MiniGamesEmporium.UI.Components;
public enum GameSection
{
    Game,
    Chat,
    Betting,
    Settings,
    Discord,
    Webview,
}

public static class GameSections
{
    public static string Label(this GameSection section) => section switch
    {
        GameSection.Game     => "Game",
        GameSection.Chat     => "Chat",
        GameSection.Betting  => "Betting",
        GameSection.Settings => "Settings",
        GameSection.Discord  => "Discord",
        GameSection.Webview  => "Webview",
        _ => throw new ArgumentOutOfRangeException(nameof(section)),
    };

    public static FontAwesomeIcon Icon(this GameSection section) => section switch
    {
        GameSection.Game     => FontAwesomeIcon.PlayCircle,
        GameSection.Chat     => FontAwesomeIcon.CommentDots,
        GameSection.Betting  => FontAwesomeIcon.Coins,
        GameSection.Settings => FontAwesomeIcon.Cog,
        GameSection.Discord  => FontAwesomeIcon.Comments,
        GameSection.Webview  => FontAwesomeIcon.Globe,
        _ => throw new ArgumentOutOfRangeException(nameof(section)),
    };
}
