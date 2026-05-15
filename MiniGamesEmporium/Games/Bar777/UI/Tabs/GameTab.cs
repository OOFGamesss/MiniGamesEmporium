using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777;
using MiniGamesEmporium.Games.Bar777.Actions;
using MiniGamesEmporium.Games.Bar777.State;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Draws the active game view for BAR 777, handling the player header display, take-bet phase controls, roll progress tracking, roll log, and session end or queue advance buttons.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Tabs;
public sealed class GameTab
{
    private static readonly Vector4 RedButton        = new(0.72f, 0.08f, 0.08f, 1f);
    private static readonly Vector4 RedButtonHovered = new(0.88f, 0.12f, 0.12f, 1f);
    private static readonly Vector4 RedButtonActive  = new(0.55f, 0.05f, 0.05f, 1f);
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly ChatQueueService chatQueue;
    public GameTab(PluginConfiguration config, SessionService sessionService, ChatQueueService chatQueue)
    {
        this.config = config;
        this.sessionService = sessionService;
        this.chatQueue = chatQueue;
    }
    public void Draw(bool skipLeadingSpacing = false)
    {
        if (!skipLeadingSpacing)
            ImGui.Spacing();
        var session = this.sessionService.GetActiveSession();
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return;
        DrawActiveSessionView(session);
    }
    private void DrawActiveSessionView(ActiveSessionState session)
    {
        if (Bar777GameIds.IsWaitingPlaceholder(session.PlayerName))
        {
            ImGui.TextDisabled($"Player: {Bar777GameIds.WaitingPlayerPlaceholder}");
            ImGui.Spacing();
            ImGui.TextWrapped("Nobody in the queue right now. Players can keyword-join, or add them manually in the queue column.");
            return;
        }
        DrawPlayerHeader(session);
        ImGui.Spacing();
        DrawTakeBetPhase(session);
        DrawRollsPhase(session);
        DrawSessionControls(session);
    }
    private void DrawPlayerHeader(ActiveSessionState session)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var startX = ImGui.GetCursorPosX();
        if (!this.config.Bar777.UseQueue && session.PlayerSet)
        {
            var fullName = BuildLockedDisplayName(session);
            const float unsetBtnW = 120f;
            var spacing = ImGui.GetStyle().ItemSpacing.X;
            ImGui.SetWindowFontScale(1.4f);
            var nameSize = ImGui.CalcTextSize(fullName);
            ImGui.SetWindowFontScale(1.0f);
            var totalW = nameSize.X + spacing + unsetBtnW;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - totalW) * 0.5f));
            ImGui.SetWindowFontScale(1.4f);
            ImGui.TextColored(EmporiumNeonTheme.Bar777Red, fullName);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button,        RedButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  RedButtonActive);
            if (ImGui.Button("Un-set Player##UnsetPlayer", new Vector2(unsetBtnW, 0)))
                this.sessionService.UnlockWalkInPlayer();
            ImGui.PopStyleColor(3);
        }
        else
        {
            var (charName, worldName) = GetCurrentTarget();
            string displayName;
            if (!this.config.Bar777.UseQueue && !string.IsNullOrEmpty(charName))
                displayName = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
            else
                displayName = Bar777GameIds.IsAnyPlaceholder(session.PlayerName) ? "Select a player to start" : BuildLockedDisplayName(session);
            ImGui.SetWindowFontScale(1.4f);
            var nameSize = ImGui.CalcTextSize(displayName);
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - nameSize.X) * 0.5f));
            ImGui.TextColored(EmporiumNeonTheme.Bar777Red, displayName);
            ImGui.SetWindowFontScale(1.0f);
        }
        var statusText   = GetStatusText(session, this.config.Bar777.Cost);
        var statusColour = GetStatusColour(session);
        ImGui.SetWindowFontScale(1.15f);
        var statusSize = ImGui.CalcTextSize(statusText);
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - statusSize.X) * 0.5f));
        ImGui.TextColored(statusColour, statusText);
        ImGui.SetWindowFontScale(1.0f);
    }
    private static string GetStatusText(ActiveSessionState session, int cost)
    {
        if (session.WinTriggered) return "WIN DETECTED!";
        if (session.RollsUsed >= session.RollsAllowed) return "Session Complete";
        if (!session.PaymentVerified)
        {
            var remaining = Math.Max(0, cost - session.AmountTraded);
            return $"Waiting for payment of {remaining:N0} gil";
        }
        return $"Rolling  {session.RollsUsed} / {session.RollsAllowed}";
    }
    private static Vector4 GetStatusColour(ActiveSessionState session)
    {
        if (session.WinTriggered) return EmporiumNeonTheme.WinGold;
        if (session.RollsUsed >= session.RollsAllowed) return EmporiumNeonTheme.SuccessMint;
        if (!session.PaymentVerified) return EmporiumNeonTheme.WarnAmber;
        return EmporiumNeonTheme.NeonCyan;
    }
    private void DrawTakeBetPhase(ActiveSessionState session)
    {
        if (session.PaymentVerified) return;
        ImGui.Separator();
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Take Bet");
        ImGui.Spacing();
        var halfW = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
        if (ImGui.Button("/tell Amount Request##TellAmtBtn", new Vector2(halfW, 0)))
        {
            if (this.config.Bar777.UseQueue)
            {
                var tellName = BuildLockedDisplayName(session);
                SendTellAmountRequest.Execute(tellName, this.config, this.chatQueue, session.AmountTraded);
            }
            else
            {
                var (charName, worldName) = GetCurrentTarget();
                if (!string.IsNullOrEmpty(charName))
                {
                    this.sessionService.LockWalkInPlayer(charName, worldName);
                    var tellName = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
                    SendTellAmountRequest.Execute(tellName, this.config, this.chatQueue, session.AmountTraded);
                }
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Trade##TradeBtn", new Vector2(halfW, 0)))
        {
            if (this.config.Bar777.UseQueue)
            {
                if (!Bar777GameIds.IsAnyPlaceholder(session.PlayerName))
                    SendTradeRequest.Execute(session.PlayerName, this.chatQueue);
            }
            else
            {
                var (charName, _) = GetCurrentTarget();
                var targetName = !string.IsNullOrEmpty(charName) ? charName
                    : session.PlayerSet ? session.PlayerName : string.Empty;
                if (!string.IsNullOrEmpty(targetName))
                    SendTradeRequest.Execute(targetName, this.chatQueue);
            }
        }
        ImGui.Spacing();
    }
    private void DrawRollsPhase(ActiveSessionState session)
    {
        if (!session.PaymentVerified) return;
        ImGui.Separator();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Rolls");
        ImGui.Spacing();
        if (session.AmountTraded > 0)
        {
            ImGui.TextColored(EmporiumNeonTheme.NeonCyan, $"Session Trade: {session.AmountTraded:N0} Gil");
            ImGui.Spacing();
        }
        if (!this.config.Bar777.Chat.AutoStartRolls)
        {
            if (ImGui.Button("Send 'Start Rolls' Msg##StartRollsMsg", new Vector2(-1, 0)))
                AnnouncePaymentReceived.Execute(session.PlayerName, this.config, this.chatQueue);
            ImGui.Spacing();
        }
        var progress = session.RollsAllowed > 0
            ? (float)session.RollsUsed / session.RollsAllowed
            : 0f;
        ImGui.Text($"Rolls: {session.RollsUsed} / {session.RollsAllowed}");
        ImGui.ProgressBar(progress, new Vector2(-1, 0), string.Empty);
        ImGui.Spacing();
        DrawRollLog(session);
        ImGui.Spacing();
    }
    private void DrawRollLog(ActiveSessionState session)
    {
        var log = session.RollLog;
        if (log == null || log.Count == 0)
        {
            ImGui.TextDisabled("No rolls yet.");
            return;
        }
        var lineH   = ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y;
        var padding = ImGui.GetStyle().WindowPadding.Y * 2f;
        var rows    = Math.Min(log.Count, 10);
        var height  = rows * lineH + padding;
        using var child = ImRaii.Child("##RollLogBox", new Vector2(-1, height), true);
        if (!child.Success) return;
        if (log.Count > 10)
        {
            using var table = ImRaii.Table("##RollGrid", 2);
            if (table.Success)
            {
                for (var row = 0; row < 10; row++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    DrawRollEntry(log, row);
                    var right = row + 10;
                    if (right < log.Count)
                    {
                        ImGui.TableSetColumnIndex(1);
                        DrawRollEntry(log, right);
                    }
                }
            }
        }
        else
        {
            for (var i = 0; i < log.Count; i++)
                DrawRollEntry(log, i);
        }
    }
    private void DrawRollEntry(System.Collections.Generic.List<int> log, int index)
    {
        var roll  = log[index];
        var isWin = roll == this.config.Bar777.WinNumber;
        var label = $"Roll {index + 1,2}: {roll,4}";
        if (isWin)
            ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{label}  WIN!");
        else
            ImGui.TextDisabled(label);
    }
    private void DrawSessionControls(ActiveSessionState session)
    {
        ImGui.Separator();
        ImGui.Spacing();
        var sessionDone = session.RollsUsed >= session.RollsAllowed || session.WinTriggered;
        if (!this.config.Bar777.UseQueue)
        {
            if (sessionDone)
            {
                var halfW = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
                if (ImGui.Button("End Game##EndWalkIn", new Vector2(halfW, 0)))
                    this.sessionService.EndWalkInAndReset();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button,        RedButton);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedButtonHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  RedButtonActive);
                if (ImGui.Button("End Session##EndSessionWalkIn", new Vector2(halfW, 0)))
                    this.sessionService.EndSessionWalkIn();
                ImGui.PopStyleColor(3);
            }
            return;
        }
        DrawQueuePlayerControls();
        ImGui.Spacing();
        if (sessionDone)
        {
            var halfW = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
            if (ImGui.Button("End Game##EndAndNext", new Vector2(halfW, 0)))
                this.sessionService.EndQueuePlayerAndProcessNext();
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button,        RedButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  RedButtonActive);
            if (ImGui.Button("End Session##EndSessionQueue", new Vector2(halfW, 0)))
                this.sessionService.EndSessionQueue();
            ImGui.PopStyleColor(3);
        }
    }
    private void DrawQueuePlayerControls()
    {
        ImGui.TextDisabled("Queue actions");
        var halfW = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
        if (ImGui.Button("To Back Q##GameToBack", new Vector2(halfW, 0)))
            this.sessionService.SendCurrentBar777ToBackOfWaitlistAndStartNext();
        ImGui.SameLine();
        if (ImGui.Button("Remove from Q##GameRemove", new Vector2(halfW, 0)))
            this.sessionService.RemoveCurrentBar777FromWaitlistAndStartNext();
    }
    private static (string CharName, string WorldName) GetCurrentTarget()
    {
        var playerChar = MiniGamesEmporium.TargetManager.Target as IPlayerCharacter;
        if (playerChar == null) return (string.Empty, string.Empty);
        var charName  = playerChar.Name.TextValue;
        var worldName = playerChar.HomeWorld.Value.Name.ToString();
        return (charName, worldName);
    }
    private static string BuildLockedDisplayName(ActiveSessionState session)
    {
        var world = session.PlayerWorld;
        return string.IsNullOrEmpty(world) ? session.PlayerName : $"{session.PlayerName}@{world}";
    }
}
