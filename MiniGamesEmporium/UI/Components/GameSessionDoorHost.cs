using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

/// <summary>Renders and sizes the centred game session door, with preset door styles.</summary>

namespace MiniGamesEmporium.UI.Components;
public sealed record GameSessionDoorStyle
{
    public float PanelContentWidth { get; init; }
    public float InnerPadX { get; init; }
    public float MinOuterContainerHeight { get; init; }
    public float ShellInsetBottom { get; init; } = 8f;
    public float MinTrackedContentSpan { get; init; }
    public float ContentBottomBleedPx { get; init; } = 14f;
    public float SeedTrackedContentSpanPx { get; init; }
}
public static class GameSessionDoorStyles
{
    public static GameSessionDoorStyle Bar777StartDoor => new()
    {
        PanelContentWidth = EmporiumNeonTheme.StartDoorPanelWidth,
        InnerPadX = EmporiumNeonTheme.StartDoorInnerPadX,
        MinOuterContainerHeight = EmporiumNeonTheme.DoorContainerMinHeight,
        MinTrackedContentSpan = 260f,
        ContentBottomBleedPx = 12f,
        SeedTrackedContentSpanPx = EmporiumNeonTheme.StartDoorEstimatedBodyHeight,
        ShellInsetBottom = 8f,
    };
    public static GameSessionDoorStyle Bar777BlockingDoor => new()
    {
        PanelContentWidth = EmporiumNeonTheme.StartDoorPanelWidth,
        InnerPadX = EmporiumNeonTheme.StartDoorInnerPadX,
        MinOuterContainerHeight = 120f,
        MinTrackedContentSpan = 96f,
        ContentBottomBleedPx = 8f,
        SeedTrackedContentSpanPx = EmporiumNeonTheme.BlockingDoorEstimatedBodyHeight,
        ShellInsetBottom = 8f,
    };
    public static GameSessionDoorStyle DeathrollTournamentStartDoor => new()
    {
        PanelContentWidth = EmporiumNeonTheme.StartDoorPanelWidth,
        InnerPadX = EmporiumNeonTheme.StartDoorInnerPadX,
        MinOuterContainerHeight = 180f,
        MinTrackedContentSpan = 160f,
        ContentBottomBleedPx = 12f,
        SeedTrackedContentSpanPx = 160f,
        ShellInsetBottom = 8f,
    };
    public static GameSessionDoorStyle DeathrollTournamentBlockingDoor => new()
    {
        PanelContentWidth = EmporiumNeonTheme.StartDoorPanelWidth,
        InnerPadX = EmporiumNeonTheme.StartDoorInnerPadX,
        MinOuterContainerHeight = 120f,
        MinTrackedContentSpan = 96f,
        ContentBottomBleedPx = 8f,
        SeedTrackedContentSpanPx = EmporiumNeonTheme.BlockingDoorEstimatedBodyHeight,
        ShellInsetBottom = 8f,
    };
    public static GameSessionDoorStyle HigherLowerStartDoor => new()
    {
        PanelContentWidth = EmporiumNeonTheme.StartDoorPanelWidth,
        InnerPadX = EmporiumNeonTheme.StartDoorInnerPadX,
        MinOuterContainerHeight = EmporiumNeonTheme.DoorContainerMinHeight,
        MinTrackedContentSpan = 260f,
        ContentBottomBleedPx = 12f,
        SeedTrackedContentSpanPx = EmporiumNeonTheme.StartDoorEstimatedBodyHeight,
        ShellInsetBottom = 8f,
    };
    public static GameSessionDoorStyle HigherLowerBlockingDoor => new()
    {
        PanelContentWidth = EmporiumNeonTheme.StartDoorPanelWidth,
        InnerPadX = EmporiumNeonTheme.StartDoorInnerPadX,
        MinOuterContainerHeight = 120f,
        MinTrackedContentSpan = 96f,
        ContentBottomBleedPx = 8f,
        SeedTrackedContentSpanPx = EmporiumNeonTheme.BlockingDoorEstimatedBodyHeight,
        ShellInsetBottom = 8f,
    };
    public static GameSessionDoorStyle RaffleStartDoor => new()
    {
        PanelContentWidth = EmporiumNeonTheme.StartDoorPanelWidth,
        InnerPadX = EmporiumNeonTheme.StartDoorInnerPadX,
        MinOuterContainerHeight = 220f,
        MinTrackedContentSpan = 200f,
        ContentBottomBleedPx = 12f,
        SeedTrackedContentSpanPx = 220f,
        ShellInsetBottom = 8f,
    };
    public static GameSessionDoorStyle RaffleBlockingDoor => new()
    {
        PanelContentWidth = EmporiumNeonTheme.StartDoorPanelWidth,
        InnerPadX = EmporiumNeonTheme.StartDoorInnerPadX,
        MinOuterContainerHeight = 120f,
        MinTrackedContentSpan = 96f,
        ContentBottomBleedPx = 8f,
        SeedTrackedContentSpanPx = EmporiumNeonTheme.BlockingDoorEstimatedBodyHeight,
        ShellInsetBottom = 8f,
    };
}
public static class KnownGameDoorModules
{
    public const string Bar777              = "Bar777";
    public const string DeathrollTournament = "DeathrollTournament";
    public const string HigherLower         = "HigherLower";
    public const string Raffle              = "Raffle";
}
public static class GameSessionDoorHost
{
    public static void Draw(
        string gameModuleId,
        string doorSurfaceId,
        ref float trackedContentSpanPx,
        GameSessionDoorStyle style,
        Action drawDoorBody)
    {
        ArgumentNullException.ThrowIfNull(gameModuleId);
        ArgumentNullException.ThrowIfNull(doorSurfaceId);
        ArgumentNullException.ThrowIfNull(drawDoorBody);
        var vpSize = ImGui.GetContentRegionAvail();
        using var shell = ImRaii.Child(
            $"##GameDoorShell_{gameModuleId}_{doorSurfaceId}",
            vpSize,
            false,
            ImGuiWindowFlags.NoScrollbar);
        if (!shell.Success)
            return;
        ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin());
        var shellAvail = ImGui.GetContentRegionAvail();
        var shellAvailX = shellAvail.X;
        var shellAvailY = shellAvail.Y;
        var shellH = MathF.Max(shellAvailY, 1f);
        var containerW = style.PanelContentWidth + 2f * style.InnerPadX;
        var targetOuterH =
            trackedContentSpanPx + style.ContentBottomBleedPx;
        var containerHDesired = MathF.Max(style.MinOuterContainerHeight, targetOuterH);
        var containerH = MathF.Min(
            containerHDesired,
            MathF.Max(style.MinOuterContainerHeight, shellH - style.ShellInsetBottom));
        var boxSize = EmporiumNeonTheme.ClampDoorBoxToShell(shellAvailX, shellAvailY, containerW, containerH);
        EmporiumNeonTheme.SetCursorCentredDoorBox(shellAvailX, shellAvailY, boxSize.X, boxSize.Y);
        using var doorBox = ImRaii.Child(
            $"##GameDoorBox_{gameModuleId}_{doorSurfaceId}",
            boxSize,
            true,
            ImGuiWindowFlags.NoScrollbar);
        if (!doorBox.Success)
            return;
        var layoutTopY = ImGui.GetCursorPosY();
        ImGui.Spacing();
        ImGui.PushItemWidth(style.PanelContentWidth);
        try
        {
            drawDoorBody();
        }
        finally
        {
            ImGui.PopItemWidth();
        }
        var span = MathF.Max(0f, ImGui.GetCursorPosY() - layoutTopY);
        trackedContentSpanPx = MathF.Max(style.MinTrackedContentSpan, span);
    }
}
