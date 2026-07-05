using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

/// <summary>Adds custom right-click menu items for player targets.</summary>

namespace MiniGamesEmporium.Events;
public sealed class PlayerContextMenuHandler : IDisposable
{
    private readonly IContextMenu contextMenu;
    private readonly List<PlayerContextMenuEntry> entries = new();

    public PlayerContextMenuHandler(IContextMenu contextMenu)
    {
        this.contextMenu = contextMenu;
        this.contextMenu.OnMenuOpened += OnMenuOpened;
    }

    public void Register(PlayerContextMenuEntry entry) => this.entries.Add(entry);

    public void Unregister(PlayerContextMenuEntry entry) => this.entries.Remove(entry);

    public void Dispose()
    {
        this.contextMenu.OnMenuOpened -= OnMenuOpened;
        this.entries.Clear();
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (args.MenuType != ContextMenuType.Default) return;
        if (args.Target is not MenuTargetDefault target) return;
        if (target.TargetObject?.ObjectKind != ObjectKind.Pc) return;

        var name        = target.TargetName;
        var worldRef    = target.TargetHomeWorld;
        var world       = worldRef.RowId != 0 ? worldRef.Value.Name.ToString() : string.Empty;
        var playerEntry = string.IsNullOrEmpty(world) ? name : $"{name}@{world}";

        foreach (var entry in this.entries)
        {
            if (!entry.IsVisible()) continue;
            args.AddMenuItem(new MenuItem
            {
                Name             = new SeString(new TextPayload(entry.Label)),
                UseDefaultPrefix = true,
                OnClicked        = _ => entry.OnSelected(playerEntry),
            });
        }
    }
}

public sealed class PlayerContextMenuEntry
{
    public required string Label { get; init; }
    public required Func<bool> IsVisible { get; init; }
    public required Action<string> OnSelected { get; init; }
}
