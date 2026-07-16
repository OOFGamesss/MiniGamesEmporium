using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System.Numerics;

/// <summary>Draws the Chat settings tab for Deathroll Tournament message templates.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollChatSettingsTab
{
    private readonly PluginConfiguration config;

    public DeathrollChatSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Deathroll Tournament");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        DrawPlaceholderReference();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawManualSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawAutoSection();
    }

    private static void DrawPlaceholderReference()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Available Placeholders");
        ImGui.Spacing();
        DrawPlaceholderRow("{buyername}",   "Buyer's full name always including @World (e.g. Jane Doe@Omega) - buyer request message only");
        DrawPlaceholderRow("{player1}",     "First player in the current match");
        DrawPlaceholderRow("{player2}",     "Second player in the current match");
        DrawPlaceholderRow("{winner}",      "Tournament winner name");
        DrawPlaceholderRow("{round}",       "Current round label  (e.g. \"Round 1\", \"Final\")");
        DrawPlaceholderRow("{prize}",       "The winner's prize - \"100,000 Gil\" for a Gil pot, or the item name / custom text otherwise");
        DrawPlaceholderRow("{entrycost}",   "Entry cost per player in Gil");
        DrawPlaceholderRow("{boostedpot}",  "Boosted pot amount in Gil");
        DrawPlaceholderRow("{playercount}", "Number of players in the tournament");
        DrawPlaceholderRow("{random10}",    "The tied /random 10 value (used in the re-roll message)");
        DrawPlaceholderRow("{firstplayer}", "The player who won the order roll and goes first");
        DrawPlaceholderRow("{roundwinner}", "The player who won a game within the best-of series");
        DrawPlaceholderRow("{matchwinner}", "The player who won the entire match (series)");
        DrawPlaceholderRow("{matchloser}",  "The player who lost the entire match (series)");
        DrawPlaceholderRow("{roundscore}",  "Current score formatted as winner-wins - loser-wins (e.g. 1 - 0)");
        DrawPlaceholderRow("{roundsleft}",  "Maximum games still playable in the series");
        DrawPlaceholderRow("{betunit}",       "Gil required per bet");
        DrawPlaceholderRow("{betkeyword}",    "The configured bet chat keyword (e.g. !bet)");
        DrawPlaceholderRow("{bettarget}",     "The entrant a bet was placed on - bet gil request message only");
        DrawPlaceholderRow("{bettingpot}",    "Total betting pot in Gil");
        DrawPlaceholderRow("{betwinners}",    "Bettors who correctly bet on the tournament winner, joined by \", \"");
    }

    private static void DrawPlaceholderRow(string token, string desc)
    {
        ImGui.TextColored(new Vector4(1f, 0.80f, 0.30f, 1f), token);
        ImGui.SameLine(110f);
        ImGui.TextDisabled(desc);
    }


    private void DrawManualSection()
    {
        var chat = this.config.DeathrollTournament.Chat;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Manual Trigger Messages");
        ImGui.Spacing();
        ImGui.TextUnformatted("Turn Reminder");
        ImGui.TextDisabled("Bell button next to a player's name in the bracket. By default it copies a /tell command to your clipboard; enable this to send a custom message instead.");
        ImGui.Spacing();
        var useCustom = chat.UseCustomTurnReminderMessage;
        if (ImGui.Checkbox("Use Custom Message##DRTurnReminderToggle", ref useCustom))
        {
            chat.UseCustomTurnReminderMessage = useCustom;
            this.config.Save();
        }
        if (useCustom)
        {
            ImGui.Spacing();
            ImGui.Indent();
            DrawMessageField(
                "Turn Reminder",
                "Sent instead of copying to clipboard when 'Use Custom Message' above is enabled.",
                "##DRTurnReminderMsg",
                () => chat.TurnReminderMessage,
                v  => { chat.TurnReminderMessage = v; this.config.Save(); });
            ImGui.Unindent();
        }
        ImGui.Spacing();
        DrawMessageField(
            "Request Gil",
            "Button: 'Request Gil' per player in the registration table.\n{player} includes the world name (e.g. Jane Doe@Omega) for cross-world /tell.",
            "##DRRequestGilMsg",
            () => this.config.DeathrollTournament.Chat.RequestGilMessage,
            v  => { this.config.DeathrollTournament.Chat.RequestGilMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Request Gil (Buyer)",
            "Button: 'Request Gil (Buyer)' in the buyer popup per player.",
            "##DRRequestGilBuyerMsg",
            () => this.config.DeathrollTournament.Chat.RequestGilBuyerMessage,
            v  => { this.config.DeathrollTournament.Chat.RequestGilBuyerMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Bracket",
            "Button: 'Announce Bracket' in the game panel.\nThe message will be sent along with the bracket matches:\n  {player1} vs {player2}",
            "##DRAnnounceBracketMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceBracketMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceBracketMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Matchup",
            "Button: 'Announce Matchup' in the match tracker.  Also used for auto-announce below.",
            "##DRAnnounceMatchupMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceMatchupMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceMatchupMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Prize",
            "Button: 'Announce Prize' in the stats panel - works for a Gil pot, item, or custom prize via {prize}.  Also used for auto-announce below.",
            "##DRAnnouncePrizeMsg",
            () => this.config.DeathrollTournament.Chat.AnnouncePrizeMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnouncePrizeMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Winner",
            "Button: 'Announce Winner' on the tournament complete screen.  Also used for auto-announce below.",
            "##DRAnnounceWinnerMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceTournamentWinnerMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceTournamentWinnerMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Re-roll Random 10",
            "Button: 'Re-roll Random 10' in the match tracker when both players tie their /random 10.  Also used for auto-announce below.",
            "##DRRerollRandom10Msg",
            () => this.config.DeathrollTournament.Chat.RerollRandom10Message,
            v  => { this.config.DeathrollTournament.Chat.RerollRandom10Message = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "First Player",
            "Button: 'First Player' in the match tracker after order rolls resolve.  Also used for auto-announce below.",
            "##DRFirstPlayerMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceFirstPlayerMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceFirstPlayerMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Round Win",
            "Button: 'Announce Round Win' in the match tracker after a game ends but the series continues.  Also used for auto-announce below.",
            "##DRRoundWinMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceRoundWinMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceRoundWinMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Match Win",
            "Button: 'Announce Match Win' in the match tracker when the series is decided.  Also used for auto-announce below.",
            "##DRMatchWinMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceMatchWinMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceMatchWinMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Betting Open",
            "Button: 'Announce Betting Open' in the Betting tab.  Also used for auto-announce below.",
            "##DRAnnounceBettingOpenMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceBettingOpenMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceBettingOpenMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Betting Pot",
            "Button: 'Announce Pot' in the Betting tab.",
            "##DRAnnounceBettingPotMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceBettingPotMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceBettingPotMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Request Bet Gil",
            "Button: 'Request Gil' next to an unpaid bet in the Betting tab.",
            "##DRRequestBetGilMsg",
            () => this.config.DeathrollTournament.Chat.RequestBetGilMessage,
            v  => { this.config.DeathrollTournament.Chat.RequestBetGilMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Bet Winner (single)",
            "Used instead of the message below when exactly one bettor correctly bet on the tournament winner and takes the whole pot.",
            "##DRAnnounceBetWinnerMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceBetWinnerMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceBetWinnerMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Bet Winners (multiple)",
            "Button: 'Announce Bet Winners' in the Betting tab once the tournament is complete.  Used when 2+ bettors split the pot.  Also used for auto-announce below.",
            "##DRAnnounceBetWinnersMsg",
            () => this.config.DeathrollTournament.Chat.AnnounceBetWinnersMessage,
            v  => { this.config.DeathrollTournament.Chat.AnnounceBetWinnersMessage = v; this.config.Save(); });
    }

    private void DrawAutoSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Auto-Send Toggles");
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnounceMatchup;
            if (ImGui.Checkbox("Auto Announce Matchup##DRAutoMatchup", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnounceMatchup = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when a new match begins");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnouncePrize;
            if (ImGui.Checkbox("Auto Announce Prize##DRAutoPrize", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnouncePrize = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when a session starts");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnounceWinner;
            if (ImGui.Checkbox("Auto Announce Winner##DRAutoWinner", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnounceWinner = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when the tournament winner is decided");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnounceRerollRandom10;
            if (ImGui.Checkbox("Auto Announce Re-roll Random 10##DRAutoReroll", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnounceRerollRandom10 = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when both players tie their /random 10");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnounceFirstPlayer;
            if (ImGui.Checkbox("Auto Announce First Player##DRAutoFirstPlayer", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnounceFirstPlayer = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when the order roll resolves and the first player is known");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnounceRoundWin;
            if (ImGui.Checkbox("Auto Announce Round Win##DRAutoRoundWin", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnounceRoundWin = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when a game ends but the series is not yet decided");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnounceMatchWin;
            if (ImGui.Checkbox("Auto Announce Match Win##DRAutoMatchWin", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnounceMatchWin = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when the series is decided and a player is eliminated");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnounceBettingOpen;
            if (ImGui.Checkbox("Auto Announce Betting Open##DRAutoBettingOpen", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnounceBettingOpen = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when a session starts with betting enabled");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.DeathrollTournament.Chat.AutoAnnounceBetWinners;
            if (ImGui.Checkbox("Auto Announce Bet Winners##DRAutoBetWinners", ref toggle))
            {
                this.config.DeathrollTournament.Chat.AutoAnnounceBetWinners = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically once the tournament winner and bet payouts are determined");
        }
    }

    private void DrawMessageField(string label, string hint, string id, System.Func<string> get, System.Action<string> set)
    {
        ImGui.TextUnformatted(label);
        ImGui.TextDisabled(hint);
        var val = get();
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText(id, ref val, 256))
            set(val);
        DrawRemovedTokenWarningIfNeeded(val);
    }

    private static void DrawRemovedTokenWarningIfNeeded(string message)
    {
        if (!message.Contains("{totalpot}")) return;
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber,
            "{totalpot} was removed and no longer does anything - use {prize} instead (it already includes \"Gil\" for a Gil pot).");
    }
}
