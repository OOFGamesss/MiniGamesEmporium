using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using MiniGamesEmporium.Actions;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Actions;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>Draws the Deathroll Tournament game view: a player registration and best-of setup screen before the tournament starts, then a left bracket board and right-side roll tracker once the tournament is under way.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollBracketTab
{
    private static readonly Vector4 PinkButton        = new(0.72f, 0.06f, 0.36f, 1f);
    private static readonly Vector4 PinkButtonHovered = new(0.90f, 0.10f, 0.48f, 1f);
    private static readonly Vector4 PinkButtonActive  = new(0.55f, 0.04f, 0.26f, 1f);
    private static readonly Vector4 GreenButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);
    private static readonly Vector4 YellowButton        = new(0.45f, 0.35f, 0.02f, 1f);
    private static readonly Vector4 YellowButtonHovered = new(0.60f, 0.48f, 0.03f, 1f);
    private static readonly Vector4 YellowButtonActive  = new(0.32f, 0.25f, 0.01f, 1f);
    private static readonly Vector4 OrangeButton        = new(0.60f, 0.25f, 0.02f, 1f);
    private static readonly Vector4 OrangeButtonHovered = new(0.80f, 0.35f, 0.03f, 1f);
    private static readonly Vector4 OrangeButtonActive  = new(0.45f, 0.18f, 0.01f, 1f);
    private static readonly Vector4 CurrentMatchBg    = new(0.28f, 0.22f, 0.02f, 1f);
    private static readonly Vector4 ResolvedMatchBg   = new(0.06f, 0.10f, 0.06f, 1f);
    private const float RightPaneW = 300f;
    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private readonly ChatQueueService chatQueue;
    private static string comboFilter = string.Empty;
    private bool openUnpaidModal = false;
    private List<string> unpaidModalPlayers = new();

    public DeathrollBracketTab(PluginConfiguration config, DeathrollTournamentService deathrollService, ChatQueueService chatQueue)
    {
        this.config           = config;
        this.deathrollService = deathrollService;
        this.chatQueue        = chatQueue;
    }

    public void Draw(bool skipLeadingSpacing = false, float reserveBottom = 0f, Action? drawStatsInline = null)
    {
        if (!skipLeadingSpacing) ImGui.Spacing();
        if (!this.deathrollService.HasActiveTournament())
        {
            DrawPreTournamentSetup(reserveBottom, drawStatsInline);
            DrawUnpaidPlayersModal();
            return;
        }
        var state = this.config.DeathrollTournamentSession!;
        if (state.TournamentWinner != null)
        {
            DrawTournamentComplete(state);
            return;
        }
        var splitH = MathF.Max(140f, ImGui.GetContentRegionAvail().Y - reserveBottom);
        using var split = ImRaii.Table("##DRSplit", 2,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1f, splitH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##DRBracketCol", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##DRTrackerCol", ImGuiTableColumnFlags.WidthFixed, RightPaneW);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        DrawBracketPane(state, splitH);
        ImGui.TableSetColumnIndex(1);
        DrawTrackerPane(state, splitH);
    }

    private void DrawPreTournamentSetup(float reserveBottom, Action? drawStatsInline)
    {
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Register Players");
        ImGui.Separator();
        ImGui.Spacing();
        var splitH = MathF.Max(60f, ImGui.GetContentRegionAvail().Y);
        using var split = ImRaii.Table("##DRPreSplit", 2,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1f, splitH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##DRPlayerCol", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##DRBestOfCol", ImGuiTableColumnFlags.WidthFixed, 220f);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        var gap         = ImGui.GetStyle().ItemSpacing.Y;
        var bottomPad   = ImGui.GetStyle().ItemSpacing.Y;
        var playerPaneH = MathF.Max(24f, splitH - reserveBottom - gap - bottomPad);
        using (var playerPane = ImRaii.Child("##DRPlayerPane", new Vector2(-1f, playerPaneH), false, ImGuiWindowFlags.NoScrollbar))
            if (playerPane.Success) DrawPlayerList();
        ImGui.Spacing();
        drawStatsInline?.Invoke();
        ImGui.TableSetColumnIndex(1);
        using (var boPane = ImRaii.Child("##DRBestOfPane", new Vector2(-1f, splitH), false))
        {
            if (!boPane.Success) return;
            DrawBestOfSettings();
            var footerH   = 1f + ImGui.GetStyle().ItemSpacing.Y * 4f + ImGui.GetFrameHeight() + ImGui.GetTextLineHeightWithSpacing();
            var remaining = ImGui.GetContentRegionAvail().Y - footerH;
            if (remaining > 0f)
                ImGui.Dummy(new Vector2(0f, remaining));
            ImGui.Separator();
            ImGui.Spacing();
            DrawStartTournamentButton();
        }
    }

    private void DrawPlayerList()
    {
        var list      = this.config.DeathrollTournament.RegisteredPlayers;
        var paidCount = this.config.DeathrollTournament.PaidPlayers.Count;
        var availH    = ImGui.GetContentRegionAvail().Y;
        var startY    = ImGui.GetCursorPosY();
        ImGui.TextColored(new Vector4(1f, 0.20f, 0.60f, 1f), $"Players ({paidCount} / {list.Count} paid)");
        ImGui.Spacing();
        DrawAddPlayerCombo();
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.DeathrollTournamentPink);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.45f, 0.75f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.72f, 0.10f, 0.40f, 1f));
        if (UIHelper.IconTextButton(FontAwesomeIcon.Random, "Shuffle", "##DRShuffle"))
            this.deathrollService.ShufflePlayers();
        ImGui.PopStyleColor(3);
        ImGui.Spacing();
        var tableH = MathF.Max(24f, availH - (ImGui.GetCursorPosY() - startY));
        if (list.Count == 0)
        {
            ImGui.TextDisabled("No players added yet.");
            return;
        }
        var spacing     = ImGui.GetStyle().ItemSpacing.X;
        var arrowColW   = UIHelper.CalcButtonSize(FontAwesomeIcon.ArrowUp, "").X * 2f + spacing;
        var actColW     = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request Gil").X
                        + UIHelper.CalcButtonSize(FontAwesomeIcon.Coins,       "Trade").X
                        + UIHelper.CalcButtonSize(FontAwesomeIcon.Times,       "Mark as Unpaid").X
                        + UIHelper.CalcButtonSize(FontAwesomeIcon.Trash,       "Remove Player").X
                        + UIHelper.CalcButtonSize(FontAwesomeIcon.Gift,        "").X
                        + spacing * 4f;
        int removeIdx = -1, togglePaidIdx = -1, targetIdx = -1, moveUpIdx = -1, moveDownIdx = -1;
        using var table = ImRaii.Table("##DRPlayerTable", 4,
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(-1f, tableH));
        if (!table.Success) return;
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("##DRArrowCl", ImGuiTableColumnFlags.WidthFixed,   arrowColW);
        ImGui.TableSetupColumn("#",           ImGuiTableColumnFlags.WidthFixed,   26f);
        ImGui.TableSetupColumn("Name",        ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##DRActCl",   ImGuiTableColumnFlags.WidthFixed,   actColW);
        ImGui.TableHeadersRow();
        for (var i = 0; i < list.Count; i++)
        {
            ImGui.TableNextRow();
            DrawPlayerTableRow(i, list[i], list.Count, ref removeIdx, ref togglePaidIdx, ref targetIdx, ref moveUpIdx, ref moveDownIdx);
        }
        if (targetIdx     >= 0) SendTradeRequest.Execute(ParseName(list[targetIdx]), this.chatQueue);
        if (togglePaidIdx >= 0) this.deathrollService.TogglePaid(list[togglePaidIdx]);
        if (moveUpIdx     >= 0) this.deathrollService.MovePlayerUp(moveUpIdx);
        if (moveDownIdx   >= 0) this.deathrollService.MovePlayerDown(moveDownIdx);
        if (removeIdx     >= 0) this.deathrollService.RemovePlayer(removeIdx);
    }

    private void DrawAddPlayerCombo()
    {
        var nearby  = NearbyPlayerList.GetSorted();
        var preview = nearby.Count == 0 ? "No players nearby" : "Add player...";
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - UIHelper.CalcButtonSize(FontAwesomeIcon.Random, "Shuffle").X - ImGui.GetStyle().ItemSpacing.X);
        using var combo = ImRaii.Combo("##DRNearbyAdd", preview, ImGuiComboFlags.HeightLarge);
        if (!combo.Success) return;
        if (nearby.Count == 0) { ImGui.TextDisabled("No players in the area."); return; }
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText("##DRNearbyFilter", ref comboFilter, 64);
        var any = false;
        foreach (var player in nearby)
        {
            if (!player.Contains(comboFilter, StringComparison.OrdinalIgnoreCase)) continue;
            any = true;
            var alreadyAdded = IsAlreadyRegistered(player);
            if (alreadyAdded)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EmporiumNeonTheme.DeathrollTournamentPink);
                ImGui.Selectable($"[Added]  {player}", false, ImGuiSelectableFlags.Disabled);
                ImGui.PopStyleColor();
            }
            else
            {
                if (!ImGui.Selectable(player)) continue;
                this.deathrollService.AddPlayer(player);
                comboFilter = string.Empty;
                ImGui.CloseCurrentPopup();
            }
        }
        if (!any) ImGui.TextDisabled("No matches.");
    }

    private void DrawPlayerTableRow(int idx, string entry, int count, ref int removeIdx, ref int togglePaidIdx, ref int targetIdx, ref int moveUpIdx, ref int moveDownIdx)
    {
        var displayName = ParseName(entry);
        var isPaid      = this.deathrollService.IsPaid(entry);
        var green       = new Vector4(0.22f, 0.82f, 0.32f, 1f);
        if (isPaid)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.05f, 0.25f, 0.10f, 1f)));
        ImGui.TableSetColumnIndex(0);
        using (ImRaii.Disabled(idx == 0))
            if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowUp, "", $"##DRUp{idx}"))
                moveUpIdx = idx;
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Move up");
        ImGui.SameLine();
        using (ImRaii.Disabled(idx == count - 1))
            if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowDown, "", $"##DRDown{idx}"))
                moveDownIdx = idx;
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Move down");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextDisabled($"{idx + 1}");
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(isPaid ? green : new Vector4(0.94f, 0.92f, 0.98f, 1f), displayName);
        ImGui.TableSetColumnIndex(3);
        var reqGilW = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request Gil").X;
        if (!isPaid)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.10f, 0.36f, 0.72f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.15f, 0.50f, 0.90f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.08f, 0.28f, 0.55f, 1f));
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil", $"##DRReqGil{idx}"))
                RequestGil.Execute(entry, this.config, this.chatQueue);
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Send a /tell requesting the entry fee");
        }
        else
        {
            ImGui.Dummy(new Vector2(reqGilW, ImGui.GetFrameHeight()));
        }
        ImGui.SameLine();
        var tradeW = UIHelper.CalcButtonSize(FontAwesomeIcon.Coins, "Trade").X;
        if (!isPaid)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.52f, 0.40f, 0.02f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.72f, 0.58f, 0.03f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.38f, 0.28f, 0.01f, 1f));
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", $"##DRTrade{idx}"))
                targetIdx = idx;
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Send trade request");
        }
        else
        {
            ImGui.Dummy(new Vector2(tradeW, ImGui.GetFrameHeight()));
        }
        ImGui.SameLine();
        var toggleBtnW = MathF.Max(
            UIHelper.CalcButtonSize(FontAwesomeIcon.Check, "Mark as Paid").X,
            UIHelper.CalcButtonSize(FontAwesomeIcon.Times, "Mark as Unpaid").X);
        if (isPaid)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.MainTabPurpleActive);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.58f, 0.16f, 0.80f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  EmporiumNeonTheme.MainTabPurple);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Mark as Unpaid", $"##DRPaid{idx}", toggleBtnW))
                togglePaidIdx = idx;
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to unmark as paid");
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.14f, 0.48f, 0.18f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.20f, 0.64f, 0.26f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.10f, 0.36f, 0.14f, 1f));
            if (UIHelper.IconTextButton(FontAwesomeIcon.Check, "Mark as Paid", $"##DRPaid{idx}", toggleBtnW))
                togglePaidIdx = idx;
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to mark as paid");
        }
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.60f, 0.08f, 0.08f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.80f, 0.12f, 0.12f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.45f, 0.06f, 0.06f, 1f));
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Remove Player", $"##DRRem{idx}") && ImGui.GetIO().KeyCtrl)
            removeIdx = idx;
        ImGui.PopStyleColor(3);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hold Ctrl and click to remove player");
        ImGui.SameLine();
        var hasBuyer = !string.IsNullOrEmpty(this.deathrollService.GetPlayerBuyer(entry));
        if (hasBuyer)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.SuccessMint with { W = 0.8f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, EmporiumNeonTheme.SuccessMint);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  EmporiumNeonTheme.SuccessMint with { W = 0.6f });
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.DeathrollTournamentPink);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.45f, 0.75f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.72f, 0.10f, 0.40f, 1f));
        }
        if (UIHelper.IconTextButton(FontAwesomeIcon.Gift, "", $"##DRBuyerBtn{idx}"))
            ImGui.OpenPopup($"##DRBuyerPopup{idx}");
        ImGui.PopStyleColor(3);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(hasBuyer ? $"Buyer: {this.deathrollService.GetPlayerBuyer(entry)}" : "Set a buyer to pay for this player");
        using var popup = ImRaii.Popup($"##DRBuyerPopup{idx}");
        if (popup.Success)
        {
            var buyer = this.deathrollService.GetPlayerBuyer(entry);
            ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, $"Buyer for {ParseName(entry)}");
            ImGui.Separator();
            ImGui.Spacing();
            if (!string.IsNullOrEmpty(buyer))
            {
                ImGui.TextDisabled("Buyer:");
                ImGui.SameLine();
                ImGui.TextColored(EmporiumNeonTheme.SuccessMint, buyer);
                ImGui.SameLine();
                if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Clear", $"##DRClearBuyer{idx}"))
                {
                    this.deathrollService.ClearPlayerBuyer(entry);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.Spacing();
                if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", $"##DRBuyerTell{idx}"))
                {
                    RequestGilBuyer.Execute(buyer, entry, this.config, this.chatQueue);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade (Buyer)", $"##DRBuyerTrade{idx}"))
                {
                    SendTradeRequest.Execute(buyer, this.chatQueue);
                    ImGui.CloseCurrentPopup();
                }
            }
            else
            {
                var (charName, charWorld) = GetCurrentTarget();
                if (!string.IsNullOrEmpty(charName))
                {
                    ImGui.TextDisabled("Targeted:");
                    ImGui.SameLine();
                    ImGui.TextUnformatted(charName);
                    ImGui.Spacing();
                    if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Set as Buyer", $"##DRSetBuyer{idx}"))
                    {
                        var fullBuyerName = string.IsNullOrEmpty(charWorld) ? charName : $"{charName}@{charWorld}";
                        this.deathrollService.SetPlayerBuyer(entry, fullBuyerName);
                    }
                }
                else
                {
                    ImGui.TextDisabled("Target a player in-game to set them as the buyer.");
                }
            }
        }
    }

    private bool IsAlreadyRegistered(string nearbyEntry)
    {
        var at = nearbyEntry.IndexOf('@');
        var nearbyName = at < 0 ? nearbyEntry : nearbyEntry[..at];
        var list = this.config.DeathrollTournament.RegisteredPlayers;
        foreach (var p in list)
        {
            var pAt = p.IndexOf('@');
            var pName = pAt < 0 ? p : p[..pAt];
            if (pName.Equals(nearbyName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void DrawBestOfSettings()
    {
        var players    = this.config.DeathrollTournament.RegisteredPlayers.Count;
        var roundCount = players >= 2 ? ComputeRoundCount(players) : 0;
        if (roundCount == 0)
        {
            ImGui.TextDisabled("Add at least 2 players to configure round settings.");
            return;
        }
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Best-of per Round");
        ImGui.Spacing();
        var list     = this.config.DeathrollTournament.BestOfPerRound;
        var prevSize = list.Count;
        while (list.Count < roundCount) list.Add(1);
        while (list.Count > roundCount) list.RemoveAt(list.Count - 1);
        if (list.Count != prevSize) this.config.Save();
        for (var i = 0; i < roundCount; i++)
        {
            var val   = list[i];
            var label = i == roundCount - 1 ? "Final" : $"Round {i + 1}";
            ImGui.SetNextItemWidth(120f);
            if (ImGui.InputInt($"{label}##DRBO{i}", ref val, 2, 2))
            {
                list[i] = Math.Max(1, val % 2 == 0 ? val + 1 : val);
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled($"Best of {list[i]}  (need {list[i] / 2 + 1} win(s))");
        }
    }

    private void DrawStartTournamentButton()
    {
        var paidCount = this.config.DeathrollTournament.PaidPlayers.Count;
        var canStart  = paidCount >= 2;
        var btnW      = UIHelper.CalcButtonSize(FontAwesomeIcon.Play, "Start Tournament").X;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - btnW) * 0.5f);
        ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
        using var disabled = ImRaii.Disabled(!canStart);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Tournament", "##DRStartTournament");
        ImGui.PopStyleColor(3);
        if (clicked)
        {
            var unpaid = this.deathrollService.GetUnpaidRegisteredPlayers();
            if (unpaid.Count > 0)
            {
                this.unpaidModalPlayers = unpaid;
                this.openUnpaidModal    = true;
            }
            else
            {
                this.deathrollService.StartTournament();
            }
        }
        if (!canStart)
        {
            ImGui.Spacing();
            const string hint = "Mark at least 2 players as paid to start.";
            ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(hint).X) * 0.5f);
            ImGui.TextDisabled(hint);
        }
    }

    private void DrawUnpaidPlayersModal()
    {
        if (this.openUnpaidModal)
        {
            ImGui.OpenPopup("Unpaid Players##DRUnpaidModal");
            this.openUnpaidModal = false;
        }
        using var modal = ImRaii.PopupModal("Unpaid Players##DRUnpaidModal", ImGuiWindowFlags.AlwaysAutoResize);
        if (!modal.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "The following players have not been marked as paid:");
        ImGui.Spacing();
        foreach (var name in this.unpaidModalPlayers)
            ImGui.BulletText(name);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        const float closeW = 80f;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - closeW) * 0.5f);
        if (ImGui.Button("Close##DRUnpaidClose", new Vector2(closeW, 0f)))
            ImGui.CloseCurrentPopup();
    }

    private void DrawBracketPane(DeathrollTournamentState state, float height)
    {
        var style      = ImGui.GetStyle();
        var roundCount = state.Rounds.Count;
        var firstCount = roundCount > 0 ? state.Rounds[0].Count : 1;
        var spacing    = style.ItemSpacing.Y;
        var paneW      = ImGui.GetContentRegionAvail().X;
        const float MinCardH = 80f;
        const float MinCardW = 120f;

        var overhead  = ImGui.GetFrameHeight() + style.ItemSpacing.Y + spacing
                      + ImGui.GetTextLineHeightWithSpacing() + spacing * 2f
                      + style.WindowPadding.Y * 2f;
        var gapH      = MathF.Max(0, firstCount - 1) * spacing;
        var matchBoxH = MathF.Max(MinCardH, MathF.Min(200f, (height - overhead - gapH) / MathF.Max(1, firstCount)));
        var colW      = MathF.Max(MinCardW, paneW / MathF.Max(1, roundCount));

        using var pane = ImRaii.Child("##DRBracketPane", new Vector2(-1f, height), false, ImGuiWindowFlags.HorizontalScrollbar);
        if (!pane.Success) return;
        DrawAnnounceBracketButton(state);
        ImGui.Spacing();
        DrawAllRounds(state, matchBoxH, colW);
    }

    private void DrawAnnounceBracketButton(DeathrollTournamentState state)
    {
        var roundLabel = AnnounceBracket.GetRoundLabel(state);
        ImGui.PushStyleColor(ImGuiCol.Button,        YellowButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellowButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellowButtonActive);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, $"Announce {roundLabel} Bracket", "##DRBracketAnn");
        ImGui.PopStyleColor(3);
        if (clicked) AnnounceBracket.Execute(this.config, this.chatQueue, state);
    }

    private void DrawAllRounds(DeathrollTournamentState state, float matchBoxH, float colW)
    {
        var roundCount = state.Rounds.Count;
        using var table = ImRaii.Table("##DRRoundTable", roundCount, ImGuiTableFlags.BordersInnerV);
        if (!table.Success) return;
        for (var r = 0; r < roundCount; r++)
            ImGui.TableSetupColumn($"##DRRound{r}", ImGuiTableColumnFlags.WidthFixed, colW);
        ImGui.TableNextRow();
        var firstRoundCount = state.Rounds[0].Count;
        for (var r = 0; r < roundCount; r++)
        {
            ImGui.TableSetColumnIndex(r);
            DrawRoundColumn(state, r, firstRoundCount, matchBoxH);
        }
    }

    private void DrawRoundColumn(DeathrollTournamentState state, int roundIdx, int firstRoundMatchCount, float matchBoxH)
    {
        var matchBoxW  = MathF.Max(120f, ImGui.GetContentRegionAvail().X - 2f);
        var bestOf     = roundIdx < state.BestOfPerRound.Count ? state.BestOfPerRound[roundIdx] : 1;
        var roundLabel = roundIdx == state.Rounds.Count - 1 ? "Final" : $"Round {roundIdx + 1}";
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, $"{roundLabel} (BO{bestOf})");
        ImGui.Separator();
        ImGui.Spacing();
        var contentStartY = ImGui.GetCursorPosY();
        var slotH         = matchBoxH + ImGui.GetStyle().ItemSpacing.Y;
        var matches       = state.Rounds[roundIdx];
        var span          = matches.Count > 0 ? firstRoundMatchCount / matches.Count : 1;
        for (var m = 0; m < matches.Count; m++)
        {
            var isCurrent = roundIdx == state.CurrentRoundIndex && m == state.CurrentMatchIndex;
            ImGui.SetCursorPosY(contentStartY + m * span * slotH + (span * slotH - matchBoxH) * 0.5f);
            DrawMatchBox(state, matches[m], isCurrent, roundIdx, m, matchBoxW, matchBoxH);
        }
    }

    private void DrawMatchBox(DeathrollTournamentState state, BracketMatch match, bool isCurrent, int roundIdx, int matchIdx, float matchBoxW, float matchBoxH)
    {
        var bgColour = match.IsResolved ? ResolvedMatchBg : (isCurrent ? CurrentMatchBg : new Vector4(0.10f, 0.04f, 0.09f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, bgColour);
        using var box = ImRaii.Child($"##DRMatch_{roundIdx}_{matchIdx}", new Vector2(matchBoxW, matchBoxH), true, ImGuiWindowFlags.NoScrollbar);
        ImGui.PopStyleColor();
        if (!box.Success) return;
        DrawMatchContent(state, match, isCurrent, matchBoxW, matchBoxH);
    }

    private static void DrawMatchContent(DeathrollTournamentState state, BracketMatch match, bool isCurrent, float boxW, float boxH)
    {
        var padX     = ImGui.GetStyle().WindowPadding.X;
        var padY     = ImGui.GetStyle().WindowPadding.Y;
        var innerW   = MathF.Max(10f, boxW - padX * 2f - 2f);
        var p1Label  = TruncateName(FormatSlot(match.Player1), innerW);
        var p2Label  = TruncateName(FormatSlot(match.Player2), innerW);
        var p1Colour = GetPlayerColour(match, isP1: true,  isCurrent, state);
        var p2Colour = GetPlayerColour(match, isP1: false, isCurrent, state);

        var lineH    = ImGui.GetTextLineHeight();
        var gapY     = ImGui.GetStyle().ItemSpacing.Y;
        var contentH = 3f * lineH + 2f * gapY;
        var startY   = padY + MathF.Max(0f, (boxH - padY * 2f - contentH) * 0.5f);
        ImGui.SetCursorPosY(startY);

        var p1Wins = isCurrent && !match.IsResolved ? state.ActiveMatchPlayer1Wins : match.Player1Wins;
        var p2Wins = isCurrent && !match.IsResolved ? state.ActiveMatchPlayer2Wins : match.Player2Wins;

        ImGui.SetCursorPosX(padX + MathF.Max(0f, (innerW - ImGui.CalcTextSize(p1Label).X) * 0.5f));
        ImGui.TextColored(p1Colour, p1Label);
        if (p1Wins > 0 || p2Wins > 0) { ImGui.SameLine(); ImGui.TextDisabled($" {p1Wins}"); }

        var vsW = ImGui.CalcTextSize("vs").X;
        ImGui.SetCursorPosX(padX + MathF.Max(0f, (innerW - vsW) * 0.5f));
        ImGui.TextDisabled("vs");

        ImGui.SetCursorPosX(padX + MathF.Max(0f, (innerW - ImGui.CalcTextSize(p2Label).X) * 0.5f));
        ImGui.TextColored(p2Colour, p2Label);
        if (p1Wins > 0 || p2Wins > 0) { ImGui.SameLine(); ImGui.TextDisabled($" {p2Wins}"); }
    }

    private static Vector4 GetPlayerColour(BracketMatch match, bool isP1, bool isCurrent, DeathrollTournamentState state)
    {
        var player = isP1 ? match.Player1 : match.Player2;
        if (string.IsNullOrEmpty(player)) return EmporiumNeonTheme.NeonCyan with { W = 0.35f };
        if (DeathrollGameIds.IsBye(player)) return new Vector4(0.4f, 0.4f, 0.4f, 0.6f);
        if (match.IsResolved && string.Equals(match.Winner, player, StringComparison.OrdinalIgnoreCase))
            return new Vector4(0.20f, 1f, 0.30f, 1f);
        if (match.IsResolved) return new Vector4(0.45f, 0.4f, 0.52f, 1f);
        if (isCurrent && !string.IsNullOrEmpty(state.CurrentTurnPlayerName))
        {
            var turnName = ParseName(state.CurrentTurnPlayerName);
            var pName    = ParseName(player);
            if (pName.Equals(turnName, StringComparison.OrdinalIgnoreCase))
                return new Vector4(1f, 0.92f, 0.15f, 1f);
        }
        return new Vector4(0.94f, 0.92f, 0.98f, 1f);
    }

    private static string FormatSlot(string slot)
    {
        if (string.IsNullOrEmpty(slot)) return "TBD";
        if (DeathrollGameIds.IsBye(slot)) return "BYE";
        return ParseName(slot);
    }

    private void DrawTrackerPane(DeathrollTournamentState state, float height)
    {
        using var pane = ImRaii.Child("##DRTrackerPane", new Vector2(-1f, height), false, ImGuiWindowFlags.NoScrollbar);
        if (!pane.Success) return;
        var match = this.deathrollService.GetCurrentMatch();
        if (match == null)
        {
            ImGui.TextDisabled("No active match.");
            return;
        }
        DrawMatchControls(state, match);
        ImGui.Spacing();
        if (state.ActiveMatchPhase == MatchPhase.DeterminingOrder || state.ActiveMatchPhase == MatchPhase.Deathrolling
            || state.ActiveMatchPhase == MatchPhase.GameOver || state.ActiveMatchPhase == MatchPhase.MatchComplete)
            DrawOrderRollSection(state, match);
        if (state.ActiveMatchPhase == MatchPhase.Deathrolling || state.ActiveMatchPhase == MatchPhase.GameOver
            || state.ActiveMatchPhase == MatchPhase.MatchComplete)
            DrawDeathrollSection(state, match);
    }

    private void DrawMatchControls(DeathrollTournamentState state, BracketMatch match)
    {
        var anyButtonDrawn = false;
        if (state.ActiveMatchPhase == MatchPhase.NotStarted)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Round", "##DRStartRound"))
                this.deathrollService.StartCurrentMatch();
            ImGui.PopStyleColor(3);
            anyButtonDrawn = true;
        }
        else if (state.ActiveMatchPhase == MatchPhase.GameOver)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        PinkButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PinkButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  PinkButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Redo, "Next Game", "##DRNextGame"))
                this.deathrollService.StartNextGame();
            ImGui.PopStyleColor(3);
            anyButtonDrawn = true;
        }
        else if (state.ActiveMatchPhase == MatchPhase.MatchComplete)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowRight, "Next Match", "##DRNextMatch"))
            {
                this.deathrollService.MoveToNextMatch();
                this.deathrollService.StartCurrentMatch();
            }
            ImGui.PopStyleColor(3);
            anyButtonDrawn = true;
        }
        if (!this.config.DeathrollTournament.Chat.AutoAnnounceMatchup)
        {
            if (anyButtonDrawn) ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button,        YellowButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellowButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellowButtonActive);
            var announceClicked = UIHelper.IconTextButton(FontAwesomeIcon.Comments, "Announce Matchup", "##DRAnnMatchup");
            ImGui.PopStyleColor(3);
            if (announceClicked) AnnounceMatchup.Execute(match.Player1, match.Player2, this.config, this.chatQueue);
            anyButtonDrawn = true;
        }
        if (state.ActiveMatchPhase == MatchPhase.DeterminingOrder
            && state.LastOrderTiedValue > 0
            && !this.config.DeathrollTournament.Chat.AutoAnnounceRerollRandom10)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        OrangeButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, OrangeButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  OrangeButtonActive);
            var rerollClicked = UIHelper.IconTextButton(FontAwesomeIcon.Dice, "Re-roll Random 10", "##DRRerollRandom10");
            ImGui.PopStyleColor(3);
            if (rerollClicked) AnnounceRerollRandom10.Execute(state.LastOrderTiedValue, this.config, this.chatQueue);
        }
        if (state.ActiveMatchPhase == MatchPhase.Deathrolling
            && state.ActiveRollLog.Count == 0
            && !this.config.DeathrollTournament.Chat.AutoAnnounceFirstPlayer)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        YellowButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellowButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellowButtonActive);
            var fpClicked = UIHelper.IconTextButton(FontAwesomeIcon.Star, "First Player", "##DRAnnFirstPlayer");
            ImGui.PopStyleColor(3);
            if (fpClicked) AnnounceFirstPlayer.Execute(state.CurrentTurnPlayerName, this.config, this.chatQueue);
            anyButtonDrawn = true;
        }
        if (!match.IsResolved
            && (state.ActiveMatchPlayer1Wins + state.ActiveMatchPlayer2Wins) > 0
            && state.ActiveMatchPhase is MatchPhase.GameOver or MatchPhase.DeterminingOrder
            && !this.config.DeathrollTournament.Chat.AutoAnnounceRoundWin)
        {
            var loserName   = ParseName(state.CurrentTurnPlayerName);
            var isLoserP1   = loserName.Equals(ParseName(match.Player1), StringComparison.OrdinalIgnoreCase);
            var roundWinner = isLoserP1 ? match.Player2 : match.Player1;
            var winnerWins  = isLoserP1 ? state.ActiveMatchPlayer2Wins : state.ActiveMatchPlayer1Wins;
            var loserWins   = isLoserP1 ? state.ActiveMatchPlayer1Wins : state.ActiveMatchPlayer2Wins;
            var roundIdx    = state.CurrentRoundIndex;
            var bestOf      = roundIdx < state.BestOfPerRound.Count ? state.BestOfPerRound[roundIdx] : 1;
            var gamesLeft   = bestOf - (state.ActiveMatchPlayer1Wins + state.ActiveMatchPlayer2Wins);
            ImGui.PushStyleColor(ImGuiCol.Button,        OrangeButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, OrangeButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  OrangeButtonActive);
            var rwClicked = UIHelper.IconTextButton(FontAwesomeIcon.Trophy, "Announce Round Win", "##DRAnnRoundWin");
            ImGui.PopStyleColor(3);
            if (rwClicked) AnnounceRoundWin.Execute(roundWinner, winnerWins, loserWins, gamesLeft, this.config, this.chatQueue);
            anyButtonDrawn = true;
        }
        if (state.ActiveMatchPhase == MatchPhase.MatchComplete
            && !this.config.DeathrollTournament.Chat.AutoAnnounceMatchWin)
        {
            var winner     = match.Winner ?? string.Empty;
            var winnerIsP1 = ParseName(winner).Equals(ParseName(match.Player1), StringComparison.OrdinalIgnoreCase);
            var loser      = winnerIsP1 ? match.Player2 : match.Player1;
            var winnerWins = winnerIsP1 ? match.Player1Wins : match.Player2Wins;
            var loserWins  = winnerIsP1 ? match.Player2Wins : match.Player1Wins;
            ImGui.PushStyleColor(ImGuiCol.Button,        PinkButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PinkButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  PinkButtonActive);
            var mwClicked = UIHelper.IconTextButton(FontAwesomeIcon.FlagCheckered, "Announce Match Win", "##DRAnnMatchWin");
            ImGui.PopStyleColor(3);
            if (mwClicked) AnnounceMatchWin.Execute(winner, loser, winnerWins, loserWins, this.config, this.chatQueue);
        }
        DrawManualWinnerButtons(state, match);
    }

    private void DrawManualWinnerButtons(DeathrollTournamentState state, BracketMatch match)
    {
        if (state.ActiveMatchPhase == MatchPhase.MatchComplete || match.IsResolved) return;
        var bestOf = state.CurrentRoundIndex < state.BestOfPerRound.Count ? state.BestOfPerRound[state.CurrentRoundIndex] : 1;
        ImGui.Spacing();
        ImGui.TextDisabled("Manual override:");
        if (bestOf >= 3)
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Check, $"{ParseName(match.Player1)} wins round", "##DRManualP1RoundWin"))
                this.deathrollService.ManuallyAddRoundWin(match.Player1);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Check, $"{ParseName(match.Player2)} wins round", "##DRManualP2RoundWin"))
                this.deathrollService.ManuallyAddRoundWin(match.Player2);
            ImGui.Spacing();
        }
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trophy, $"{ParseName(match.Player1)} wins match", "##DRManualP1Win"))
            this.deathrollService.ManuallySetWinner(match.Player1);
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trophy, $"{ParseName(match.Player2)} wins match", "##DRManualP2Win"))
            this.deathrollService.ManuallySetWinner(match.Player2);
    }

    private static void DrawOrderRollSection(DeathrollTournamentState state, BracketMatch match)
    {
        ImGui.Separator();
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Order Roll  (/random 10)");
        ImGui.Spacing();
        var p1Roll = state.OrderRollPlayer1;
        var p2Roll = state.OrderRollPlayer2;
        var p1Colour = p1Roll > 0 ? EmporiumNeonTheme.SuccessMint : EmporiumNeonTheme.NeonCyan;
        var p2Colour = p2Roll > 0 ? EmporiumNeonTheme.SuccessMint : EmporiumNeonTheme.NeonMagenta;
        ImGui.TextColored(p1Colour, $"{ParseName(match.Player1)}: {(p1Roll > 0 ? p1Roll.ToString() : "waiting...")}");
        ImGui.TextColored(p2Colour, $"{ParseName(match.Player2)}: {(p2Roll > 0 ? p2Roll.ToString() : "waiting...")}");
        if (state.LastOrderTiedValue > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"Tie on {state.LastOrderTiedValue}! Both players must re-roll /random 10.");
        }
        else if (p1Roll > 0 && p2Roll > 0)
        {
            ImGui.Spacing();
            var goesFirst = p1Roll >= p2Roll ? ParseName(match.Player1) : ParseName(match.Player2);
            ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{goesFirst} goes first!");
        }
        ImGui.Spacing();
    }

    private static void DrawDeathrollSection(DeathrollTournamentState state, BracketMatch match)
    {
        ImGui.Separator();
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Deathroll Log");
        ImGui.Spacing();
        if (state.ActiveMatchPhase == MatchPhase.Deathrolling && !string.IsNullOrEmpty(state.CurrentTurnPlayerName))
        {
            var maxDisplay = state.CurrentDeathrollMax == 0 ? 1000 : state.CurrentDeathrollMax;
            ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{ParseName(state.CurrentTurnPlayerName)} to roll /random {maxDisplay}");
        }
        ImGui.Spacing();
        var log = state.ActiveRollLog;
        if (log.Count == 0)
        {
            ImGui.TextDisabled("No rolls yet.");
            return;
        }
        var logH = MathF.Max(60f, ImGui.GetContentRegionAvail().Y - 8f);
        using var logBox = ImRaii.Child("##DRRollLog", new Vector2(-1f, logH), true);
        if (!logBox.Success) return;
        for (var i = log.Count - 1; i >= 0; i--)
        {
            var entry  = log[i];
            var colour = entry.RollValue == 1 ? EmporiumNeonTheme.Bar777Red : new Vector4(0.94f, 0.92f, 0.98f, 1f);
            ImGui.TextColored(colour, $"{entry.PlayerName}  /random {entry.RollMax}  ->  {entry.RollValue}{(entry.RollValue == 1 ? "  DEAD!" : string.Empty)}");
        }
    }

    private void DrawTournamentComplete(DeathrollTournamentState state)
    {
        ImGui.Spacing();
        ImGui.SetWindowFontScale(1.5f);
        var winnerName = ParseName(state.TournamentWinner ?? string.Empty);
        ImGui.TextColored(EmporiumNeonTheme.WinGold, $"Tournament Winner: {winnerName}!");
        ImGui.SetWindowFontScale(1.0f);
        ImGui.Spacing();
        var pot = this.deathrollService.ComputeTotalPot();
        ImGui.TextColored(EmporiumNeonTheme.WinGold, $"Pot: {pot:N0} Gil");
        if (!this.config.DeathrollTournament.Chat.AutoAnnounceWinner)
        {
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Button,        YellowButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellowButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellowButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Winner", "##DRAnnWinner"))
                AnnounceTournamentWinner.Execute(state.TournamentWinner ?? string.Empty, pot, this.config, this.chatQueue);
            ImGui.PopStyleColor(3);
        }
    }

    private static int ComputeRoundCount(int playerCount)
    {
        var size  = NextPowerOf2(playerCount);
        var count = 0;
        while (size > 1) { size >>= 1; count++; }
        return count;
    }

    private static int NextPowerOf2(int n)
    {
        if (n <= 1) return 2;
        var p = 1;
        while (p < n) p <<= 1;
        return p;
    }

    private static string ParseName(string entry)
    {
        var at = entry.IndexOf('@');
        return at < 0 ? entry.Trim() : entry[..at].Trim();
    }

    private static string TruncateName(string text, float maxW)
    {
        if (ImGui.CalcTextSize(text).X <= maxW) return text;
        const string ellipsis = "...";
        var budget = maxW - ImGui.CalcTextSize(ellipsis).X;
        if (budget <= 0f) return ellipsis;
        var trimmed = text;
        while (trimmed.Length > 0 && ImGui.CalcTextSize(trimmed).X > budget)
            trimmed = trimmed[..^1];
        return trimmed + ellipsis;
    }
    private static (string CharName, string WorldName) GetCurrentTarget()
    {
        var playerChar = MiniGamesEmporium.TargetManager.Target as IPlayerCharacter;
        if (playerChar == null) return (string.Empty, string.Empty);
        return (playerChar.Name.TextValue, playerChar.HomeWorld.Value.Name.ToString());
    }
}
