using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

/// <summary>Subscribes to the Dalamud context menu and adds custom items for each registered entry when the target is a player character.</summary>

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
        var world       = target.TargetHomeWorld.Value.Name.ToString();
        var playerEntry = string.IsNullOrEmpty(world) ? name : $"{name}@{world}";

        foreach (var entry in this.entries)
        {
            if (!entry.IsVisible()) continue;
            args.AddMenuItem(new MenuItem
            {
                Name      = new SeString(new TextPayload(entry.Label)),
                OnClicked = _ => entry.OnSelected(playerEntry),
            });
        }
    }
}
