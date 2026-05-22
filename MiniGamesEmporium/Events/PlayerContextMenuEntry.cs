using System;

/// <summary>A single entry that can be registered with PlayerContextMenuHandler to add a custom item to the player right-click context menu.</summary>

namespace MiniGamesEmporium.Events;
public sealed class PlayerContextMenuEntry
{
    public required string Label { get; init; }
    public required Func<bool> IsVisible { get; init; }
    public required Action<string> OnSelected { get; init; }
}
