using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Actions;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

/// <summary>Draws the Betting tab for Deathroll Tournament: declaring, correcting and reviewing bets, and payout tracking.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollBetsTab
{
    private static readonly Vector4 YellButtonColour        = new(0.72f, 0.55f, 0.00f, 1f);
    private static readonly Vector4 YellButtonColourHovered = new(0.88f, 0.68f, 0.00f, 1f);
    private static readonly Vector4 YellButtonColourActive  = new(0.58f, 0.44f, 0.00f, 1f);
    private static readonly Vector4 LinkButtonColour        = new(0.45f, 0.35f, 0.02f, 1f);
    private static readonly Vector4 LinkButtonColourHovered = new(0.60f, 0.48f, 0.03f, 1f);
    private static readonly Vector4 LinkButtonColourActive  = new(0.32f, 0.25f, 0.01f, 1f);
    private static readonly Vector4 BlueButtonColour        = new(0.10f, 0.36f, 0.72f, 1f);
    private static readonly Vector4 BlueButtonColourHovered = new(0.15f, 0.50f, 0.90f, 1f);
    private static readonly Vector4 BlueButtonColourActive  = new(0.08f, 0.28f, 0.55f, 1f);
    private static readonly Vector4 GreenButtonColour        = new(0.14f, 0.48f, 0.18f, 1f);
    private static readonly Vector4 GreenButtonColourHovered = new(0.20f, 0.64f, 0.26f, 1f);
    private static readonly Vector4 GreenButtonColourActive  = new(0.10f, 0.36f, 0.14f, 1f);
    private static readonly Vector4 RedButtonColour        = new(0.60f, 0.08f, 0.08f, 1f);
    private static readonly Vector4 RedButtonColourHovered = new(0.80f, 0.12f, 0.12f, 1f);
    private static readonly Vector4 RedButtonColourActive  = new(0.45f, 0.06f, 0.06f, 1f);
    private static readonly Vector4 GoldColour             = new(1f, 0.84f, 0f, 1f);
    private const float TrophySide = 140f;
    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private readonly DeathrollBettingService bettingService;
    private readonly ChatQueueService chatQueue;
    private readonly AutoPayoutService autoPayoutService;
    private readonly ISharedImmediateTexture? trophyTexture;
    private string bettorSelection = string.Empty;
    private string bettorFilter = string.Empty;
    private string targetSelection = string.Empty;
    private string fixTargetFilter = string.Empty;

    public DeathrollBetsTab(PluginConfiguration config, DeathrollTournamentService deathrollService, DeathrollBettingService bettingService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService)
    {
        this.config            = config;
        this.deathrollService  = deathrollService;
        this.bettingService    = bettingService;
        this.chatQueue         = chatQueue;
        this.autoPayoutService = autoPayoutService;
        var path = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "trophy.png");
        if (File.Exists(path))
            this.trophyTexture = MiniGamesEmporium.TextureProvider.GetFromFile(path);
    }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Betting");
        ImGui.Separator();
        ImGui.Spacing();
        if (!this.deathrollService.IsSessionActive())
        {
            ImGui.TextDisabled("Start a session to configure betting.");
            return;
        }
        if (!this.bettingService.IsBettingEnabledForSession())
        {
            ImGui.TextDisabled("Betting is disabled for this session.");
            return;
        }
        var panelH  = GetPotPanelHeight();
        var scrollH = MathF.Max(60f, ImGui.GetContentRegionAvail().Y - panelH - ImGui.GetStyle().ItemSpacing.Y);
        using (var scroll = ImRaii.Child("##DeathrollBetsScroll", new Vector2(-1f, scrollH), false))
        {
            if (scroll.Success)
            {
                var state = this.deathrollService.GetState();
                if (state != null && state.TournamentWinner != null)
                {
                    DrawPayoutSummary(state);
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                }
                if (!this.deathrollService.HasActiveTournament())
                {
                    DrawAddBetRow();
                    ImGui.Spacing();
                }
                DrawUnresolvedSection();
                DrawBetsTable();
            }
        }
        ImGui.Spacing();
        DrawPotSummary();
    }

    private void DrawPotSummary()
    {
        var pot      = this.bettingService.ComputeBettingPot();
        var betUnit  = this.bettingService.GetBetUnit();
        var betCount = this.bettingService.ComputeConfirmedBetCount();
        using var panel = ImRaii.Child("##DRBettingPotPanel", new Vector2(-1f, GetPotPanelHeight()), true, ImGuiWindowFlags.NoScrollbar);
        if (!panel.Success) return;
        using var table = ImRaii.Table("##DRBettingPotTable", 3, ImGuiTableFlags.None, new Vector2(-1f, 0f));
        if (!table.Success) return;
        ImGui.TableSetupColumn("##DRBPLabel",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##DRBPAction", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("##DRBPValue",  ImGuiTableColumnFlags.WidthFixed, 160f);
        DrawStatRow("Betting Pot", $"{pot:N0} Gil", EmporiumNeonTheme.WinGold,
            FontAwesomeIcon.Bullhorn, "Announce Pot", "##DRAnnounceBettingPot",
            () => AnnounceBettingPot.Execute(this.config, this.chatQueue, pot));
        DrawStatRow("Bet Cost", $"{betUnit:N0} Gil", EmporiumNeonTheme.NeonCyan,
            FontAwesomeIcon.Bullhorn, "Announce Open", "##DRAnnounceBettingOpen",
            () => AnnounceBettingOpen.Execute(this.config, this.chatQueue));
        DrawPlainStatRow("Bet Count", betCount.ToString(), EmporiumNeonTheme.NeonMagenta);
    }

    private static float GetPotPanelHeight()
    {
        var rowH = ImGui.GetTextLineHeight() + ImGui.GetStyle().CellPadding.Y * 2f;
        return 3 * rowH + ImGui.GetStyle().WindowPadding.Y * 2f + 4f;
    }

    private static void DrawStatRow(string label, string value, Vector4 valueColour, FontAwesomeIcon icon, string buttonLabel, string buttonId, Action onClick)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(1);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4f, 0f));
        ImGui.PushStyleColor(ImGuiCol.Button,        YellButtonColour);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellButtonColourHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellButtonColourActive);
        var clicked = UIHelper.IconTextButton(icon, buttonLabel, buttonId);
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar();
        if (clicked) onClick();
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(valueColour, value);
    }

    private static void DrawPlainStatRow(string label, string value, Vector4 valueColour)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(valueColour, value);
    }

    private void DrawAddBetRow()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Add Bet");
        ImGui.Spacing();
        var addBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.UserPlus, "Add Bet").X;
        var comboW  = MathF.Max(120f, (ImGui.GetContentRegionAvail().X - addBtnW - ImGui.GetStyle().ItemSpacing.X * 2f) / 2f);

        DrawBettorCombo(comboW);
        ImGui.SameLine();
        DrawTargetCombo(comboW);
        ImGui.SameLine();
        using (ImRaii.Disabled(string.IsNullOrWhiteSpace(this.bettorSelection) || string.IsNullOrWhiteSpace(this.targetSelection)))
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        LinkButtonColour);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, LinkButtonColourHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  LinkButtonColourActive);
            var addClicked = UIHelper.IconTextButton(FontAwesomeIcon.UserPlus, "Add Bet", "##DRAddBet");
            ImGui.PopStyleColor(3);
            if (addClicked)
            {
                this.bettingService.DeclareBet(this.bettorSelection, this.targetSelection, fromChat: false);
                this.bettorSelection = string.Empty;
                this.targetSelection = string.Empty;
            }
        }
    }

    private void DrawBettorCombo(float width)
    {
        var nearby  = PlayerInfoService.GetNearbySorted();
        var preview = string.IsNullOrEmpty(this.bettorSelection)
            ? (nearby.Count == 0 ? "No players nearby" : "Bettor...")
            : PlayerInfoService.StripWorld(this.bettorSelection);
        ImGui.SetNextItemWidth(width);
        using var combo = ImRaii.Combo("##DRBetBettor", preview, ImGuiComboFlags.HeightLarge);
        if (!combo.Success) return;
        if (nearby.Count == 0) { ImGui.TextDisabled("No players in the area."); return; }
        if (ImGui.IsWindowAppearing()) { this.bettorFilter = string.Empty; ImGui.SetKeyboardFocusHere(); }
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText("##DRBetBettorFilter", ref this.bettorFilter, 64);
        var any = false;
        foreach (var player in nearby)
        {
            if (!player.Contains(this.bettorFilter, StringComparison.OrdinalIgnoreCase)) continue;
            any = true;
            if (!ImGui.Selectable(player)) continue;
            this.bettorSelection = player;
            this.bettorFilter    = string.Empty;
            ImGui.CloseCurrentPopup();
        }
        if (!any) ImGui.TextDisabled("No matches.");
    }

    private void DrawTargetCombo(float width)
    {
        var registered    = this.config.DeathrollTournament.RegisteredPlayers;
        var targetPreview = string.IsNullOrEmpty(this.targetSelection) ? "Target..." : PlayerInfoService.StripWorld(this.targetSelection);
        ImGui.SetNextItemWidth(width);
        using var combo = ImRaii.Combo("##DRBetTarget", targetPreview);
        if (!combo.Success) return;
        if (registered.Count == 0) ImGui.TextDisabled("No registered players yet.");
        foreach (var p in registered)
        {
            if (!ImGui.Selectable(PlayerInfoService.StripWorld(p))) continue;
            this.targetSelection = p;
        }
    }

    private void DrawBetsTable()
    {
        var bets = this.config.DeathrollTournament.Bets;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, $"Bets ({bets.Count(b => b.IsPaid)} / {bets.Count} paid)");
        ImGui.Spacing();
        if (bets.Count == 0)
        {
            ImGui.TextDisabled("No bets placed yet.");
            return;
        }
        var removeId = Guid.Empty;
        var toggleId = Guid.Empty;
        var tableH   = MathF.Min(260f, bets.Count * 30f + 40f);
        var toggleBtnW = MathF.Max(
            UIHelper.CalcButtonSize(FontAwesomeIcon.Check, "Mark as Paid").X,
            UIHelper.CalcButtonSize(FontAwesomeIcon.Times, "Mark as Unpaid").X);
        using var table = ImRaii.Table("##DRBetsTable", 5,
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(-1f, tableH));
        if (!table.Success) return;
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Bettor", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthFixed, 70f);
        ImGui.TableSetupColumn("Paid",   ImGuiTableColumnFlags.WidthFixed, toggleBtnW + 8f);
        ImGui.TableSetupColumn("##DRBetAct", ImGuiTableColumnFlags.WidthFixed, 76f);
        ImGui.TableHeadersRow();
        foreach (var bet in bets)
        {
            ImGui.TableNextRow();
            if (bet.IsPaid)
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.05f, 0.25f, 0.10f, 1f)));
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(PlayerInfoService.StripWorld(bet.BettorName));
            ImGui.TableSetColumnIndex(1);
            if (bet.NeedsReview)
                ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"{bet.TargetName} [unresolved]");
            else
                ImGui.TextUnformatted(PlayerInfoService.StripWorld(bet.TargetName));
            ImGui.TableSetColumnIndex(2);
            ImGui.TextDisabled(bet.FromChat ? "Chat" : "Manual");
            ImGui.TableSetColumnIndex(3);
            if (bet.IsPaid)
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.MainTabPurpleActive);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.58f, 0.16f, 0.80f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  EmporiumNeonTheme.MainTabPurple);
                if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Mark as Unpaid", $"##DRBetPaid{bet.Id}", toggleBtnW))
                    toggleId = bet.Id;
                ImGui.PopStyleColor(3);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        GreenButtonColour);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonColourHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonColourActive);
                if (UIHelper.IconTextButton(FontAwesomeIcon.Check, "Mark as Paid", $"##DRBetPaid{bet.Id}", toggleBtnW))
                    toggleId = bet.Id;
                ImGui.PopStyleColor(3);
            }
            ImGui.TableSetColumnIndex(4);
            if (!bet.IsPaid && !bet.NeedsReview)
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        BlueButtonColour);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, BlueButtonColourHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  BlueButtonColourActive);
                if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "", $"##DRBetReqGil{bet.Id}"))
                    RequestBetGil.Execute(bet.BettorName, bet.TargetName, this.config, this.chatQueue);
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Send a /tell requesting the bet gil");
                ImGui.SameLine();
            }
            ImGui.PushStyleColor(ImGuiCol.Button,        RedButtonColour);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedButtonColourHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  RedButtonColourActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "", $"##DRBetRem{bet.Id}") && ImGui.GetIO().KeyCtrl)
                removeId = bet.Id;
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hold Ctrl and click to remove");
        }
        if (toggleId != Guid.Empty)
        {
            var bet = bets.First(b => b.Id == toggleId);
            this.bettingService.SetBetPaid(toggleId, !bet.IsPaid);
        }
        if (removeId != Guid.Empty)
            this.bettingService.RemoveBet(removeId);
    }

    private void DrawUnresolvedSection()
    {
        var unresolved = this.bettingService.GetUnresolvedBets();
        if (unresolved.Count == 0) return;
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"Needs Attention ({unresolved.Count})");
        ImGui.TextDisabled("These bets will not count toward the pot until resolved and paid.");
        ImGui.Spacing();
        var registered = this.config.DeathrollTournament.RegisteredPlayers;
        foreach (var bet in unresolved)
        {
            var needsTargetFix = bet.NeedsReview || !registered.Any(p => DeathrollTournamentService.NamesMatch(p, bet.TargetName));
            ImGui.TextUnformatted(PlayerInfoService.StripWorld(bet.BettorName));
            ImGui.SameLine();
            ImGui.TextDisabled("bet on");
            ImGui.SameLine();
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, string.IsNullOrEmpty(bet.TargetName) ? "(no target)" : bet.TargetName);
            if (!needsTargetFix && !bet.IsPaid)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(unpaid)");
            }
            ImGui.SameLine();
            if (needsTargetFix)
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        LinkButtonColour);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, LinkButtonColourHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  LinkButtonColourActive);
                var fixClicked = UIHelper.IconTextButton(FontAwesomeIcon.Wrench, "Fix Target", $"##DRFixBet{bet.Id}");
                ImGui.PopStyleColor(3);
                if (fixClicked)
                {
                    this.fixTargetFilter = string.Empty;
                    ImGui.OpenPopup($"##DRFixBetPopup{bet.Id}");
                }
                DrawFixTargetPopup(bet);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        GreenButtonColour);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonColourHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonColourActive);
                var markPaidClicked = UIHelper.IconTextButton(FontAwesomeIcon.Check, "Mark Paid", $"##DRMarkPaidBet{bet.Id}");
                ImGui.PopStyleColor(3);
                if (markPaidClicked)
                    this.bettingService.SetBetPaid(bet.Id, true);
            }
        }
        ImGui.Spacing();
    }

    private void DrawFixTargetPopup(DeathrollBet bet)
    {
        using var popup = ImRaii.Popup($"##DRFixBetPopup{bet.Id}");
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, $"Target for {PlayerInfoService.StripWorld(bet.BettorName)}");
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(200f);
        ImGui.InputText("##DRFixTargetFilter", ref this.fixTargetFilter, 64);
        ImGui.Spacing();
        var registered = this.config.DeathrollTournament.RegisteredPlayers;
        var any = false;
        foreach (var p in registered)
        {
            var name = PlayerInfoService.StripWorld(p);
            if (!name.Contains(this.fixTargetFilter, StringComparison.OrdinalIgnoreCase)) continue;
            any = true;
            if (!ImGui.Selectable(name)) continue;
            this.bettingService.CorrectBetTarget(bet.Id, p);
            ImGui.CloseCurrentPopup();
        }
        if (!any) ImGui.TextDisabled("No matches.");
        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Button,        RedButtonColour);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedButtonColourHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  RedButtonColourActive);
        var removeClicked = UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Remove Bet", $"##DRFixBetRemove{bet.Id}");
        ImGui.PopStyleColor(3);
        if (removeClicked)
        {
            this.bettingService.RemoveBet(bet.Id);
            ImGui.CloseCurrentPopup();
        }
    }

    private void DrawPayoutSummary(DeathrollTournamentState state)
    {
        ImGui.Spacing();
        var avail  = ImGui.GetContentRegionAvail().X;
        var startX = ImGui.GetCursorPosX();
        var winnerName = PlayerInfoService.StripWorld(state.TournamentWinner ?? string.Empty);

        ImGui.SetWindowFontScale(1.6f);
        var nameW = ImGui.CalcTextSize(winnerName).X;
        ImGui.SetWindowFontScale(1.0f);
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - nameW) * 0.5f));
        ImGui.SetWindowFontScale(1.6f);
        ImGui.TextColored(GoldColour, winnerName);
        ImGui.SetWindowFontScale(1.0f);

        var subtitle = state.BetPayouts.Count switch
        {
            0 => "NO WINNING BETS",
            1 => "BET WINNER!",
            _ => "BET WINNERS!",
        };
        var subtitleW = ImGui.CalcTextSize(subtitle).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - subtitleW) * 0.5f));
        ImGui.TextColored(GoldColour, subtitle);

        ImGui.Spacing();

        var trophySide = TrophySide * ImGuiHelpers.GlobalScale;
        var tex = this.trophyTexture?.GetWrapOrDefault();
        if (tex != null)
        {
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - trophySide) * 0.5f));
            ImGui.Image(tex.Handle, new Vector2(trophySide, trophySide));
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (state.BetPayouts.Count == 0)
        {
            var msg  = $"No bets were placed on {winnerName} - the {state.BettingPotAtStart:N0} Gil betting pot has no payout recipients.";
            var msgW = ImGui.CalcTextSize(msg).X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - msgW) * 0.5f));
            ImGui.TextDisabled(msg);
            return;
        }

        var pot = state.BettingPotAtStart;

        var potLabel = $"Pot: {pot:N0} Gil";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(potLabel).X) * 0.5f));
        ImGui.TextColored(GoldColour, potLabel);

        if (state.BetPayouts.Count > 1)
        {
            var shareLabel = $"{state.BetPayouts.Count} winners - {state.BetPayouts[0].ShareGil:N0} Gil each";
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(shareLabel).X) * 0.5f));
            ImGui.TextColored(EmporiumNeonTheme.NeonCyan, shareLabel);
        }

        ImGui.Spacing();

        if (!this.config.DeathrollTournament.Chat.AutoAnnounceBetWinners)
        {
            var annBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Bullhorn, "Announce Bet Winners").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - annBtnW) * 0.5f));
            ImGui.PushStyleColor(ImGuiCol.Button,        YellButtonColour);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellButtonColourHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellButtonColourActive);
            var announceClicked = UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Bet Winners", "##DRAnnounceBetWinners");
            ImGui.PopStyleColor(3);
            if (announceClicked && state.TournamentWinner != null)
            {
                var betWinners = state.BetPayouts.Select(p => p.BettorName).ToList();
                AnnounceBetWinners.Execute(state.TournamentWinner, state.BettingPotAtStart, betWinners, this.config, this.chatQueue);
            }
            ImGui.Spacing();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        for (var i = 0; i < state.BetPayouts.Count; i++)
        {
            DrawPayoutCard(state.BetPayouts[i], winnerName, avail, startX);
            if (i < state.BetPayouts.Count - 1)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
        }

        var remainder = state.BettingPotAtStart - state.BetPayouts.Sum(p => p.ShareGil);
        if (remainder > 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled($"Unallocated remainder: {remainder:N0} gil (rounding)");
        }
    }

    private void DrawPayoutCard(DeathrollBetPayout payout, string tournamentWinnerName, float avail, float startX)
    {
        var name      = PlayerInfoService.StripWorld(payout.BettorName);
        var remaining = Math.Max(0L, payout.ShareGil - payout.PaidGil);

        ImGui.SetWindowFontScale(1.3f);
        var nameW = ImGui.CalcTextSize(name).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - nameW) * 0.5f));
        ImGui.TextColored(GoldColour, name);
        ImGui.SetWindowFontScale(1.0f);

        var subtitle = $"Correctly bet on {tournamentWinnerName}!";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(subtitle).X) * 0.5f));
        ImGui.TextColored(GoldColour, subtitle);

        ImGui.Spacing();

        var labelColW = MathF.Max(ImGui.CalcTextSize("Share:").X, MathF.Max(ImGui.CalcTextSize("Paid:").X, ImGui.CalcTextSize("Remaining:").X));
        var valueColW = MathF.Max(ImGui.CalcTextSize($"{payout.ShareGil:N0} Gil").X, MathF.Max(ImGui.CalcTextSize($"{payout.PaidGil:N0} Gil").X, ImGui.CalcTextSize($"{remaining:N0} Gil").X));
        var spacing   = ImGui.GetStyle().ItemSpacing.X;
        var blockW    = labelColW + spacing + valueColW;
        var rowX      = startX + MathF.Max(0f, (avail - blockW) * 0.5f);
        var valueX    = rowX + labelColW + spacing;

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(GoldColour, "Share:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(GoldColour, $"{payout.ShareGil:N0} Gil");

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, "Paid:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, $"{payout.PaidGil:N0} Gil");

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Remaining:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"{remaining:N0} Gil");

        ImGui.Spacing();

        var tradeBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Coins, "Trade").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - tradeBtnW) * 0.5f));
        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", $"##DRBetTrade{payout.BettorName}"))
                SendTradeRequest.Execute(name, this.chatQueue);
        }

        ImGui.Spacing();
        DrawAutoPayoutButton(payout, name, remaining, avail, startX);
        ImGui.Spacing();
        DrawPayoutProgressBar(payout.ShareGil, payout.PaidGil, avail, startX);
    }

    private void DrawAutoPayoutButton(DeathrollBetPayout payout, string name, long remaining, float avail, float startX)
    {
        if (this.autoPayoutService.IsRunningFor(name))
        {
            var stopBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, "Stop Auto Payout").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - stopBtnW) * 0.5f));
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Auto Payout", $"##DRBetStopAuto{payout.BettorName}"))
                this.autoPayoutService.Stop();
            return;
        }
        using var disabled = ImRaii.Disabled(remaining <= 0 || (this.autoPayoutService.IsRunning && !this.autoPayoutService.IsRunningFor(name)));
        var autoBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.MoneyBillWave, "Auto Payout").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - autoBtnW) * 0.5f));
        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.MoneyBillWave, "Auto Payout", $"##DRBetAutoPayout{payout.BettorName}"))
        {
            this.autoPayoutService.Start(
                name,
                () =>
                {
                    var payouts = this.deathrollService.GetState()?.BetPayouts;
                    var current = payouts?.FirstOrDefault(p => p.BettorName.Equals(payout.BettorName, StringComparison.OrdinalIgnoreCase));
                    return current != null ? Math.Max(0L, current.ShareGil - current.PaidGil) : 0L;
                },
                () => this.deathrollService.IsSessionActive());
        }
    }

    private static void DrawPayoutProgressBar(long share, long paid, float avail, float startX)
    {
        var progress   = share > 0 ? MathF.Min(1f, (float)paid / share) : 1f;
        var pctOverlay = $"{progress * 100f:F0}% paid out";
        ImGui.SetCursorPosX(startX);
        ImGui.ProgressBar(progress, new Vector2(avail, ImGui.GetFrameHeight()), pctOverlay);
    }
}
