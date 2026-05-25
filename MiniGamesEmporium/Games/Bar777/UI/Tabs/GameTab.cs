using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Games.Bar777.Actions;
using MiniGamesEmporium.Games.Bar777.State;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Draws the active game view for BAR 777, handling the player header display, take-bet phase controls (two-column: collect payment left, buyer right), roll progress tracking, roll log, and session end or queue advance buttons.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Tabs;
public sealed class GameTab
{
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly ChatQueueService chatQueue;
    private int _pendingRollCount;
    private int _lastKnownAmountTraded = -1;
    private DateTime _lastKnownSessionStart;

    public GameTab(PluginConfiguration config, SessionService sessionService, ChatQueueService chatQueue)
    {
        this.config         = config;
        this.sessionService = sessionService;
        this.chatQueue      = chatQueue;
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
        var avail  = ImGui.GetContentRegionAvail().X;
        var startX = ImGui.GetCursorPosX();

        if (!this.config.Bar777.UseQueue && session.PlayerSet)
        {
            var fullName       = BuildLockedDisplayName(session);
            var spacing        = ImGui.GetStyle().ItemSpacing.X;
            var unsetBtnSize   = UIHelper.CalcButtonSize(FontAwesomeIcon.UserSlash, "Un-set Player");
            ImGui.SetWindowFontScale(1.4f);
            var nameSize = ImGui.CalcTextSize(fullName);
            ImGui.SetWindowFontScale(1.0f);
            var totalW = nameSize.X + spacing + unsetBtnSize.X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - totalW) * 0.5f));
            ImGui.SetWindowFontScale(1.4f);
            ImGui.TextColored(EmporiumNeonTheme.SuccessMint, fullName);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.SameLine();
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.UserSlash, "Un-set Player", "##UnsetPlayer"))
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
            ImGui.SetWindowFontScale(1.0f);

            var showNextBtn = this.config.Bar777.UseQueue && !Bar777GameIds.IsAnyPlaceholder(session.PlayerName);
            var showSetBtn  = !this.config.Bar777.UseQueue && !string.IsNullOrEmpty(charName);
            float cursorX;
            if (showNextBtn)
            {
                var btnSize = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Next Player Up");
                cursorX = startX + MathF.Max(0f, (avail - nameSize.X - ImGui.GetStyle().ItemSpacing.X - btnSize.X) * 0.5f);
            }
            else if (showSetBtn)
            {
                var btnSize = UIHelper.CalcButtonSize(FontAwesomeIcon.UserCheck, "Set Player");
                cursorX = startX + MathF.Max(0f, (avail - nameSize.X - ImGui.GetStyle().ItemSpacing.X - btnSize.X) * 0.5f);
            }
            else
            {
                cursorX = startX + MathF.Max(0f, (avail - nameSize.X) * 0.5f);
            }

            ImGui.SetCursorPosX(cursorX);
            ImGui.SetWindowFontScale(1.4f);
            ImGui.TextColored(EmporiumNeonTheme.Bar777Red, displayName);
            ImGui.SetWindowFontScale(1.0f);

            if (showNextBtn)
            {
                ImGui.SameLine();
                using var yellow = UIHelper.PushYellowButtonColours();
                if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Next Player Up", "##NextPlayerUpBtn"))
                    AnnounceNextPlayerUp.Execute(displayName, this.config, this.chatQueue);
            }
            else if (showSetBtn)
            {
                ImGui.SameLine();
                using var green = UIHelper.PushGreenButtonColours();
                if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Set Player", "##SetWalkInPlayer"))
                    this.sessionService.LockWalkInPlayer(charName, worldName);
            }
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
        ImGui.Spacing();
        {
            using var split = ImRaii.Table("##TakeBetSplit", 2, ImGuiTableFlags.BordersInnerV, new Vector2(-1f, 0f));
            if (split.Success)
            {
                ImGui.TableSetupColumn("##TakeBetLeft",  ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##TakeBetRight", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                DrawPrimaryBetActions(session);
                ImGui.TableSetColumnIndex(1);
                DrawBuyerSection(session);
            }
        }
        ImGui.Spacing();
        DrawStartGameSection(session);
    }

    private void DrawPrimaryBetActions(ActiveSessionState session)
    {
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;

        var headerText = "Take Bet";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(headerText).X) * 0.5f));
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, headerText);
        ImGui.Spacing();

        var reqGilW = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request Gil").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - reqGilW) * 0.5f));
        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil", "##TellAmtBtn"))
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
        }

        ImGui.Spacing();

        var tradeW = UIHelper.CalcButtonSize(FontAwesomeIcon.ArrowRightArrowLeft, "Trade").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - tradeW) * 0.5f));
        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", "##TradeBtn"))
            {
                if (this.config.Bar777.UseQueue)
                {
                    if (!Bar777GameIds.IsAnyPlaceholder(session.PlayerName))
                        SendTradeRequest.Execute(session.PlayerName, this.chatQueue);
                }
                else
                {
                    var (charName, _) = GetCurrentTarget();
                    var targetName    = !string.IsNullOrEmpty(charName) ? charName
                                      : session.PlayerSet ? session.PlayerName : string.Empty;
                    if (!string.IsNullOrEmpty(targetName))
                        SendTradeRequest.Execute(targetName, this.chatQueue);
                }
            }
        }
    }

    private void SyncPendingRolls(ActiveSessionState session)
    {
        if (session.AmountTraded == this._lastKnownAmountTraded && session.StartedAt == this._lastKnownSessionStart) return;
        var costPerRoll = this.config.Bar777.CostPerRoll;
        this._pendingRollCount     = costPerRoll > 0
            ? Math.Min(session.AmountTraded / costPerRoll, this.config.Bar777.MaxRolls)
            : 0;
        this._lastKnownAmountTraded = session.AmountTraded;
        this._lastKnownSessionStart = session.StartedAt;
    }

    private void DrawBuyerSection(ActiveSessionState session)
    {
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;
        var style  = ImGui.GetStyle();

        var headerText = "Paying for another player";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(headerText).X) * 0.5f));
        ImGui.TextColored(EmporiumNeonTheme.NeonMagenta, headerText);
        ImGui.Spacing();

        var buyer = this.sessionService.GetBuyer();
        if (!string.IsNullOrEmpty(buyer))
        {
            var clearBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Times, "Clear").X;
            var rowW      = ImGui.CalcTextSize("Buyer:").X + style.ItemSpacing.X
                          + ImGui.CalcTextSize(buyer).X    + style.ItemSpacing.X
                          + clearBtnW;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - rowW) * 0.5f));
            ImGui.TextDisabled("Buyer:");
            ImGui.SameLine();
            ImGui.TextColored(EmporiumNeonTheme.SuccessMint, buyer);
            ImGui.SameLine();
            using (UIHelper.PushRedButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Clear", "##ClearBuyer"))
                    this.sessionService.ClearBuyer();
            }
            ImGui.Spacing();

            var reqGilBuyerW = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - reqGilBuyerW) * 0.5f));
            using (UIHelper.PushBlueButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", "##BuyerRequestGil"))
                    SendTellBuyerRequest.Execute(buyer, session.PlayerName, this.config, this.chatQueue);
            }
            ImGui.Spacing();

            var tradeBuyerW = UIHelper.CalcButtonSize(FontAwesomeIcon.ArrowRightArrowLeft, "Trade (Buyer)").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - tradeBuyerW) * 0.5f));
            using (UIHelper.PushAmberButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade (Buyer)", "##BuyerTrade"))
                    SendTradeRequest.Execute(buyer, this.chatQueue);
            }
        }
        else
        {
            var (charName, worldName) = GetCurrentTarget();
            if (!string.IsNullOrEmpty(charName))
            {
                var targetedRowW = ImGui.CalcTextSize("Targeted:").X + style.ItemSpacing.X + ImGui.CalcTextSize(charName).X;
                ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - targetedRowW) * 0.5f));
                ImGui.TextDisabled("Targeted:");
                ImGui.SameLine();
                ImGui.TextUnformatted(charName);
                ImGui.Spacing();

                var setBuyerW = UIHelper.CalcButtonSize(FontAwesomeIcon.UserCheck, "Set as Buyer").X;
                ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - setBuyerW) * 0.5f));
                using (UIHelper.PushGreenButtonColours())
                {
                    if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Set as Buyer", "##SetBuyer"))
                    {
                        var fullName = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
                        this.sessionService.SetBuyer(fullName);
                    }
                }
            }
            else
            {
                const string hintText = "Target a player in-game to set them as the buyer.";
                ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(hintText).X) * 0.5f));
                ImGui.TextDisabled(hintText);
            }
        }
        ImGui.Spacing();
    }

    private void DrawStartGameSection(ActiveSessionState session)
    {
        SyncPendingRolls(session);
        ImGui.Separator();
        ImGui.Spacing();
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;

        var headerText = "Start Game";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(headerText).X) * 0.5f));
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, headerText);
        ImGui.Spacing();

        string descText;
        if (session.AmountTraded > 0)
        {
            var costPerRoll = this.config.Bar777.CostPerRoll;
            var calculated  = costPerRoll > 0 ? Math.Min(session.AmountTraded / costPerRoll, this.config.Bar777.MaxRolls) : 0;
            descText = $"{session.AmountTraded:N0} Gil received - {calculated} roll(s) at {costPerRoll:N0}/roll";
        }
        else
        {
            descText = "No trade recorded. Enter roll count manually.";
        }
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(descText).X) * 0.5f));
        ImGui.TextDisabled(descText);
        ImGui.Spacing();

        const float InputW = 160f;
        var rollsLabelW   = ImGui.CalcTextSize("Rolls").X;
        var inputGroupW   = InputW + ImGui.GetStyle().ItemInnerSpacing.X + rollsLabelW;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - inputGroupW) * 0.5f));
        ImGui.SetNextItemWidth(InputW);
        ImGui.InputInt("Rolls##PendingRolls", ref this._pendingRollCount, 1, 1);
        this._pendingRollCount = Math.Clamp(this._pendingRollCount, 0, this.config.Bar777.MaxRolls);

        var noPlayer = !this.config.Bar777.UseQueue && !session.PlayerSet;
        if (noPlayer)
        {
            const string hintText = "Target a player first to lock them in.";
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(hintText).X) * 0.5f));
            ImGui.TextDisabled(hintText);
        }
        ImGui.Spacing();

        var startBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Play, "Start Game").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - startBtnW) * 0.5f));
        using var disabled = ImRaii.Disabled(this._pendingRollCount < 1 || noPlayer);
        using var green    = UIHelper.PushGreenButtonColours();
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
            using (UIHelper.PushYellowButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Comments, "Send 'Start Rolls' Msg", "##StartRollsMsg"))
                    AnnouncePaymentReceived.Execute(session.PlayerName, this.config, this.chatQueue);
            }
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
            using (UIHelper.PushRedButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.ExclamationTriangle, "End Game Early", "##EndEarlyBtn"))
                    ImGui.OpenPopup("EndEarlyConfirm##Modal");
            }
            using var modal = ImRaii.PopupModal("EndEarlyConfirm##Modal");
            if (modal.Success)
            {
                ImGui.TextUnformatted("The player hasn't finished their rolls.");
                ImGui.TextUnformatted("Are you sure you want to end their game?");
                ImGui.Spacing();
                using (UIHelper.PushGreenButtonColours())
                {
                    if (UIHelper.IconTextButton(FontAwesomeIcon.Check, "Yes, end early", "##ConfirmEndEarly"))
                    {
                        if (this.config.Bar777.UseQueue)
                            this.sessionService.EndQueuePlayerAndProcessNext();
                        else
                            this.sessionService.EndWalkInAndReset();
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.SameLine();
                using (UIHelper.PushRedButtonColours())
                {
                    if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##CancelEndEarly"))
                        ImGui.CloseCurrentPopup();
                }
            }
            ImGui.Spacing();
        }
        if (!this.config.Bar777.UseQueue)
        {
            if (sessionDone)
            {
                using var green = UIHelper.PushGreenButtonColours();
                if (UIHelper.IconTextButton(FontAwesomeIcon.FlagCheckered, "End Game", "##EndWalkIn"))
                    this.sessionService.EndWalkInAndReset();
            }
            return;
        }
        if (sessionDone)
        {
            using var green = UIHelper.PushGreenButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.FlagCheckered, "End Game", "##EndAndNext"))
                this.sessionService.EndQueuePlayerAndProcessNext();
        }
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
