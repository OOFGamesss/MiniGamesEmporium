using System.Collections.Generic;

/// <summary>In-memory panel visibility state, kept for the plugin session only and never saved to disk.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class UiLayoutState
{
    public static bool HideNavSidebar { get; set; } = false;

    private static readonly Dictionary<string, bool> CollapsedPanels = new();

    public static bool IsCollapsed(string panelKey) =>
        CollapsedPanels.TryGetValue(panelKey, out var collapsed) && collapsed;

    public static void SetCollapsed(string panelKey, bool collapsed)
    {
        if (collapsed) CollapsedPanels[panelKey] = true;
        else CollapsedPanels.Remove(panelKey);
    }

    public static void Toggle(string panelKey) => SetCollapsed(panelKey, !IsCollapsed(panelKey));
}
