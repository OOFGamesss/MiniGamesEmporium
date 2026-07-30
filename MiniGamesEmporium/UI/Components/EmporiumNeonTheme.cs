using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

/// <summary>Defines the neon colour palette, layout constants, and ImGui theme scopes.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class EmporiumNeonTheme
{
    public static readonly Vector4 Bar777Red = new(0.98f, 0.12f, 0.28f, 1f);
    public static readonly Vector4 Bar777RedDim = new(0.55f, 0.08f, 0.18f, 1f);
    public static readonly Vector4 MinefieldGreen = new(0.1f, 0.98f, 0.3f, 1f);
    public static readonly Vector4 MinefieldGreenDim = new(0.06f, 0.55f, 0.18f, 1f);
    public static readonly Vector4 PauseYellowDim = new(0.55f, 0.50f, 0.06f, 1f);
    public static readonly Vector4 HigherLowerOrange = new(1f, 0.55f, 0.10f, 1f);
    public static readonly Vector4 HigherLowerOrangeDim = new(0.55f, 0.30f, 0.05f, 1f);
    public static readonly Vector4 BeerPongWhite = new(0.95f, 0.95f, 0.95f, 1f);
    public static readonly Vector4 BeerPongWhiteDim = new(0.55f, 0.55f, 0.55f, 1f);
    public static readonly Vector4 DartsBlue = new(0.15f, 0.55f, 1f, 1f);
    public static readonly Vector4 DartsBlueDim = new(0.08f, 0.28f, 0.55f, 1f);
    public static readonly Vector4 EightBallPoolPurple = new(0.70f, 0.15f, 1f, 1f);
    public static readonly Vector4 EightBallPoolPurpleDim = new(0.38f, 0.08f, 0.55f, 1f);
    public static readonly Vector4 RussianRouletteCyan = new(0.2f, 0.98f, 0.95f, 1f);
    public static readonly Vector4 RussianRouletteCyanDim = new(0.10f, 0.55f, 0.52f, 1f);
    public static readonly Vector4 DeathrollTournamentPink = new(1f, 0.20f, 0.60f, 1f);
    public static readonly Vector4 DeathrollTournamentPinkDim = new(0.55f, 0.10f, 0.32f, 1f);
    public static readonly Vector4 RaidBossFuchsia = new(0.95f, 0.05f, 0.78f, 1f);
    public static readonly Vector4 RaidBossFuchsiaDim = new(0.52f, 0.03f, 0.42f, 1f);
    public static readonly Vector4 DealOrNoDealGold = new(1f, 0.72f, 0.06f, 1f);
    public static readonly Vector4 DealOrNoDealGoldDim = new(0.55f, 0.40f, 0.03f, 1f);
    public static readonly Vector4 VotingMadnessLime = new(0.72f, 1f, 0.08f, 1f);
    public static readonly Vector4 VotingMadnessLimeDim = new(0.40f, 0.55f, 0.04f, 1f);
    public static readonly Vector4 RaffleTeal = new(0.10f, 0.85f, 0.72f, 1f);
    public static readonly Vector4 RaffleTealDim = new(0.05f, 0.46f, 0.40f, 1f);
    public static readonly Vector4 NeonCyan = new(0.2f, 0.98f, 0.95f, 1f);
    public static readonly Vector4 NeonMagenta = new(0.95f, 0.25f, 0.85f, 1f);
    public static readonly Vector4 WinGold = new(1f, 0.92f, 0.2f, 1f);
    public static readonly Vector4 SuccessMint = new(0.35f, 1f, 0.55f, 1f);
    public static readonly Vector4 WarnAmber = new(1f, 0.55f, 0.15f, 1f);
    public static readonly Vector4 WarningPanel = new(1f, 0.72f, 0.42f, 1f);
    public static readonly Vector4 EmporiumPurple = new(0.22f, 0.07f, 0.36f, 1f);
    public static readonly Vector4 EmporiumPurpleHovered = new(0.32f, 0.10f, 0.50f, 1f);
    public static readonly Vector4 EmporiumPurpleActive = new(0.44f, 0.12f, 0.66f, 1f);
    public const float StartDoorPanelWidth = 420f;
    public const float StartDoorEstimatedBodyHeight = 400f;
    public const float BlockingDoorEstimatedBodyHeight = 180f;
    public const float StartDoorInnerPadX = 18f;
    public const float DoorContainerMinHeight = 240f;
    public readonly struct Scope : IDisposable
    {
        private readonly int colours;
        private readonly int vars;
        public Scope()
        {
            this.colours = PushColours();
            this.vars = PushVars();
        }
        public void Dispose()
        {
            ImGui.PopStyleVar(this.vars);
            ImGui.PopStyleColor(this.colours);
        }
    }
    public static void BeginCentredGroup(float nominalInnerWidth)
    {
        var minX = ImGui.GetWindowContentRegionMin().X;
        var maxX = ImGui.GetWindowContentRegionMax().X;
        var fullW = MathF.Max(0f, maxX - minX);
        var indent = MathF.Max(0f, (fullW - nominalInnerWidth) * 0.5f);
        ImGui.SetCursorPosX(minX + indent);
        ImGui.BeginGroup();
    }
    public static Vector2 ClampDoorBoxToShell(float shellAvailX, float shellAvailY, float desiredW, float desiredH)
    {
        return new Vector2(
            MathF.Min(desiredW, MathF.Max(0f, shellAvailX)),
            MathF.Min(desiredH, MathF.Max(0f, shellAvailY)));
    }
    public static void SetCursorCentredDoorBox(float shellAvailX, float shellAvailY, float boxW, float boxH)
    {
        var min = ImGui.GetWindowContentRegionMin();
        var ox = min.X + MathF.Max(0f, (shellAvailX - boxW) * 0.5f);
        var oy = min.Y + MathF.Max(0f, (shellAvailY - boxH) * 0.5f);
        ImGui.SetCursorPos(new Vector2(ox, oy));
    }
    public static void EndCentredGroup()
    {
        ImGui.EndGroup();
        var y = ImGui.GetCursorPosY();
        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMin().X);
        ImGui.SetCursorPosY(y);
    }
    private static int PushVars()
    {
        var n = 0;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        n++;
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8f);
        n++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
        n++;
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        n++;
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 8f);
        n++;
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 8f);
        n++;
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 4f);
        n++;
        return n;
    }
    private static int PushColours()
    {
        var n = 0;
        var deep = new Vector4(0.04f, 0.02f, 0.07f, 0.97f);
        var panel = new Vector4(0.07f, 0.04f, 0.11f, 1f);
        var frame = new Vector4(0.11f, 0.05f, 0.09f, 1f);
        var frameH = new Vector4(0.2f, 0.06f, 0.12f, 1f);
        var frameA = new Vector4(Bar777RedDim.X + 0.15f, 0.05f, 0.09f, 1f);
        Push(ImGuiCol.WindowBg, deep);
        n++;
        Push(ImGuiCol.ChildBg, panel);
        n++;
        Push(ImGuiCol.PopupBg, panel);
        n++;
        Push(ImGuiCol.Border, new Vector4(0.42f, 0.08f, 0.62f, 0.60f));
        n++;
        Push(ImGuiCol.FrameBg, frame);
        n++;
        Push(ImGuiCol.FrameBgHovered, frameH);
        n++;
        Push(ImGuiCol.FrameBgActive, frameA);
        n++;
        Push(ImGuiCol.TitleBg, deep);
        n++;
        Push(ImGuiCol.TitleBgActive, new Vector4(0.22f, 0.06f, 0.34f, 1f));
        n++;
        Push(ImGuiCol.ScrollbarBg, new Vector4(0.03f, 0.02f, 0.05f, 1f));
        n++;
        Push(ImGuiCol.ScrollbarGrab, Bar777RedDim);
        n++;
        Push(ImGuiCol.ScrollbarGrabHovered, Bar777Red);
        n++;
        Push(ImGuiCol.ScrollbarGrabActive, new Vector4(1f, 0.22f, 0.38f, 1f));
        n++;
        Push(ImGuiCol.CheckMark, Bar777Red);
        n++;
        Push(ImGuiCol.SliderGrab, NeonCyan);
        n++;
        Push(ImGuiCol.SliderGrabActive, new Vector4(0.85f, 1f, 0.98f, 1f));
        n++;
        Push(ImGuiCol.Button, new Vector4(0.14f, 0.04f, 0.08f, 1f));
        n++;
        Push(ImGuiCol.ButtonHovered, new Vector4(Bar777Red.X * 0.75f, 0.08f, 0.2f, 1f));
        n++;
        Push(ImGuiCol.ButtonActive, new Vector4(Bar777Red.X, 0.1f, 0.26f, 1f));
        n++;
        Push(ImGuiCol.Header, new Vector4(0.16f, 0.05f, 0.1f, 1f));
        n++;
        Push(ImGuiCol.HeaderHovered, new Vector4(0.35f, 0.06f, 0.14f, 1f));
        n++;
        Push(ImGuiCol.HeaderActive, new Vector4(0.48f, 0.08f, 0.18f, 1f));
        n++;
        Push(ImGuiCol.Separator, new Vector4(NeonCyan.X * 0.5f, NeonCyan.Y * 0.5f, NeonCyan.Z * 0.5f, 0.35f));
        n++;
        Push(ImGuiCol.SeparatorHovered, new Vector4(NeonMagenta.X, NeonMagenta.Y, NeonMagenta.Z, 0.55f));
        n++;
        Push(ImGuiCol.SeparatorActive, NeonMagenta);
        n++;
        Push(ImGuiCol.ResizeGrip, new Vector4(Bar777Red.X, Bar777Red.Y, Bar777Red.Z, 0.25f));
        n++;
        Push(ImGuiCol.ResizeGripHovered, new Vector4(Bar777Red.X, Bar777Red.Y, Bar777Red.Z, 0.55f));
        n++;
        Push(ImGuiCol.ResizeGripActive, Bar777Red);
        n++;
        Push(ImGuiCol.PlotHistogram, new Vector4(NeonMagenta.X * 0.9f, NeonMagenta.Y * 0.7f, NeonMagenta.Z, 0.85f));
        n++;
        Push(ImGuiCol.PlotHistogramHovered, NeonMagenta);
        n++;
        Push(ImGuiCol.Text, new Vector4(0.94f, 0.92f, 0.98f, 1f));
        n++;
        Push(ImGuiCol.TextDisabled, new Vector4(0.45f, 0.4f, 0.52f, 1f));
        n++;
        return n;
        static void Push(ImGuiCol col, Vector4 rgba)
        {
            ImGui.PushStyleColor(col, rgba);
        }
    }
}
