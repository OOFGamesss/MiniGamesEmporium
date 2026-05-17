using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
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
    private const float ButtonW = 150f;
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly ChatQueueService chatQueue;
    private int _pendingRollCount;
    private int _lastKnownAmountTraded = -1;
    private DateTime _lastKnownSessionStart;
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
            var spacing = ImGui.GetStyle().ItemSpacing.X;
            ImGui.SetWindowFontScale(1.4f);
            var nameSize = ImGui.CalcTextSize(fullName);
            ImGui.SetWindowFontScale(1.0f);
            var totalW = nameSize.X + spacing + ButtonW;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - totalW) * 0.5f));
            ImGui.SetWindowFontScale(1.4f);
            ImGui.TextColored(EmporiumNeonTheme.SuccessMint, fullName);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, RedButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, RedButtonActive);
            var unsetClicked = UIHelper.IconTextButton(FontAwesomeIcon.UserSlash, "Un-set Player", "##UnsetPlayer");
            ImGui.PopStyleColor(3);
            if (unsetClicked)
                this.sessionService.UnlockWalkInPlayer();
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
        var statusText   = GetStatusText(session);
        var statusColour = GetStatusColour(session);
        ImGui.SetWindowFontScale(1.15f);
        var statusSize = ImGui.CalcTextSize(statusText);
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - statusSize.X) * 0.5f));
        ImGui.TextColored(statusColour, statusText);
        ImGui.SetWindowFontScale(1.0f);
    }
    private static string GetStatusText(ActiveSessionState session)
    {
        if (session.WinTriggered) return "WIN DETECTED!";
        if (session.RollsUsed >= session.RollsAllowed) return "Session Complete";
        if (!session.PaymentVerified)
            return session.AmountTraded > 0
                ? $"Received {session.AmountTraded:N0} Gil - set rolls and start"
                : "Awaiting payment - set rolls and start";
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
        if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Request Gil", "##TellAmtBtn"))
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
        if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowRightArrowLeft, "Trade", "##TradeBtn"))
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
        DrawStartGameSection(session);
    }
    private void SyncPendingRolls(ActiveSessionState session)
    {
        if (session.AmountTraded == this._lastKnownAmountTraded && session.StartedAt == this._lastKnownSessionStart) return;
        var costPerRoll = this.config.Bar777.CostPerRoll;
        this._pendingRollCount = costPerRoll > 0
            ? Math.Min(session.AmountTraded / costPerRoll, this.config.Bar777.MaxRolls)
            : 0;
        this._lastKnownAmountTraded = session.AmountTraded;
        this._lastKnownSessionStart = session.StartedAt;
    }
    private void DrawStartGameSection(ActiveSessionState session)
    {
        SyncPendingRolls(session);
        ImGui.Separator();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Start Game");
        ImGui.Spacing();
        if (session.AmountTraded > 0)
        {
            var costPerRoll = this.config.Bar777.CostPerRoll;
            var calculated  = costPerRoll > 0 ? Math.Min(session.AmountTraded / costPerRoll, this.config.Bar777.MaxRolls) : 0;
            ImGui.TextDisabled($"{session.AmountTraded:N0} Gil received - {calculated} roll(s) at {costPerRoll:N0}/roll");
        }
        else
        {
            ImGui.TextDisabled("No trade recorded. Enter roll count manually.");
        }
        ImGui.Spacing();
        ImGui.SetNextItemWidth(160f);
        ImGui.InputInt("Rolls##PendingRolls", ref this._pendingRollCount, 1, 1);
        this._pendingRollCount = Math.Clamp(this._pendingRollCount, 0, this.config.Bar777.MaxRolls);
        var noPlayer = !this.config.Bar777.UseQueue && !session.PlayerSet;
        if (noPlayer)
            ImGui.TextDisabled("Target a player first to lock them in.");
        using var disabled = ImRaii.Disabled(this._pendingRollCount < 1 || noPlayer);
        if (UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Game", "##StartGame"))
            this.sessionService.StartGameWithRolls(this._pendingRollCount);
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
            if (UIHelper.IconTextButton(FontAwesomeIcon.Comments, "Send 'Start Rolls' Msg", "##StartRollsMsg"))
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
        const int maxRows = 5;
        var lineH   = ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y;
        var padding = ImGui.GetStyle().WindowPadding.Y * 2f;
        var visRows = Math.Min(log.Count, maxRows);
        var height  = visRows * lineH + padding;
        using var child = ImRaii.Child("##RollLogBox", new Vector2(-1, height), true);
        if (!child.Success) return;
        var numCols = Math.Max(1, (log.Count + maxRows - 1) / maxRows);
        using var table = ImRaii.Table("##RollGrid", numCols);
        if (!table.Success) return;
        int? deleteIndex = null;
        for (var row = 0; row < maxRows; row++)
        {
            ImGui.TableNextRow();
            for (var col = 0; col < numCols; col++)
            {
                var idx = col * maxRows + row;
                if (idx >= log.Count) continue;
                ImGui.TableSetColumnIndex(col);
                if (DrawRollEntry(log, idx))
                    deleteIndex = idx;
            }
        }
        if (deleteIndex.HasValue)
            this.sessionService.RemoveRoll(deleteIndex.Value);
    }
    private bool DrawRollEntry(System.Collections.Generic.List<int> log, int index)
    {
        var roll  = log[index];
        var isWin = roll == this.config.Bar777.WinNumber;
        var label = $"Roll {index + 1,2}: {roll,4}";
        if (isWin)
            ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{label}  WIN!");
        else
            ImGui.TextDisabled(label);
        var deleteRequested = false;
        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            ImGui.TextUnformatted("Ctrl+Click to remove this roll");
        }
        if (ImGui.IsItemClicked() && ImGui.GetIO().KeyCtrl)
            deleteRequested = true;
        return deleteRequested;
    }
    private void DrawSessionControls(ActiveSessionState session)
    {
        ImGui.Separator();
        ImGui.Spacing();
        var sessionDone = session.RollsUsed >= session.RollsAllowed || session.WinTriggered;
        if (session.PaymentVerified && !sessionDone)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, RedButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, RedButtonActive);
            var endEarlyClicked = UIHelper.IconTextButton(FontAwesomeIcon.ExclamationTriangle, "End Game Early", "##EndEarlyBtn");
            ImGui.PopStyleColor(3);
            if (endEarlyClicked)
                ImGui.OpenPopup("EndEarlyConfirm##Modal");
            using var modal = ImRaii.PopupModal("EndEarlyConfirm##Modal");
            if (modal.Success)
            {
                ImGui.TextUnformatted("The player hasn't finished their rolls.");
                ImGui.TextUnformatted("Are you sure you want to end their game?");
                ImGui.Spacing();
                if (UIHelper.IconTextButton(FontAwesomeIcon.Check, "Yes, end early", "##ConfirmEndEarly"))
                {
                    if (this.config.Bar777.UseQueue)
                        this.sessionService.EndQueuePlayerAndProcessNext();
                    else
                        this.sessionService.EndWalkInAndReset();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##CancelEndEarly"))
                    ImGui.CloseCurrentPopup();
            }
            ImGui.Spacing();
        }
        if (!this.config.Bar777.UseQueue)
        {
            if (sessionDone)
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.FlagCheckered, "End Game", "##EndWalkIn"))
                    this.sessionService.EndWalkInAndReset();
            }
            return;
        }
        if (!session.PaymentVerified || sessionDone)
        {
            DrawQueuePlayerControls();
            ImGui.Spacing();
        }
        if (sessionDone)
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.FlagCheckered, "End Game", "##EndAndNext"))
                this.sessionService.EndQueuePlayerAndProcessNext();
        }
    }
    private void DrawQueuePlayerControls()
    {
        ImGui.Separator();
        ImGui.TextColored(EmporiumNeonTheme.MinefieldGreen, "Queue actions");
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Redo, "To Back Q", "##GameToBack"))
            this.sessionService.SendCurrentBar777ToBackOfWaitlistAndStartNext();
        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.UserMinus, "Remove from Q", "##GameRemove"))
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
