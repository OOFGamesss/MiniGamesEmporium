using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Settings tab for Coin Collector.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.UI.Tabs;
public sealed class CoinCollectorSettingsTab
{
    private const float DelayFieldWidth = 180f;

    private static readonly Vector4 CardAccent = EmporiumNeonTheme.CoinCollectorIndigo;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();
    private readonly ThemedCard automationCard = new();

    private readonly PluginConfiguration config;

    public CoinCollectorSettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Coin Collector");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();

        this.automationCard.Draw("##CCAutomationCard", "Automation", CardAccent, CardTitle, DrawAutomationBody);

        if (this.config.CoinCollectorActiveSession != null)
        {
            SessionLockNotice.Draw(
                "A session is active. Session defaults are locked until you stop the session.");
            return;
        }
        this.card.Draw("##CCSettingsCard", "Session Defaults", CardAccent, CardTitle, DrawSettingsBody);
    }

    private void DrawAutomationBody()
    {
        var cc = this.config.CoinCollector;

        var trade = cc.TradeOnRequestGil;
        if (SettingToggle.Draw("Trade with Request Gil", "##CCAutoTradeRequest",
                "opens the trade window as soon as Request Gil is clicked", ref trade))
        {
            cc.TradeOnRequestGil = trade;
            this.config.Save();
        }
        ImGui.Spacing();

        var autoBegin = cc.AutoBeginOnPayment;
        if (SettingToggle.Draw("Auto Begin on Payment", "##CCAutoBegin",
                "begins the turn and tells the player to roll once the entry fee lands", ref autoBegin))
        {
            cc.AutoBeginOnPayment = autoBegin;
            this.config.Save();
        }
        ImGui.Spacing();

        var autoEnd = cc.AutoEndTurn;
        if (SettingToggle.Draw("Auto End Turn", "##CCAutoEndTurn",
                "returns to the player list once a turn finishes, or starts their next paid attempt", ref autoEnd))
        {
            cc.AutoEndTurn = autoEnd;
            this.config.Save();
        }

        if (cc.AutoEndTurn)
        {
            ImGui.Spacing();
            var delay = cc.AutoEndTurnDelayMs;
            if (SettingToggle.DrawIntField("Auto End Turn Delay (ms)", "##CCAutoEndDelay",
                    "How long the Turn Complete card stays up before the list returns.",
                    ref delay, 0, 30_000, 500, DelayFieldWidth))
            {
                cc.AutoEndTurnDelayMs = delay;
                this.config.Save();
            }
        }
        ImGui.Spacing();

        var multiple = cc.AllowMultipleAttempts;
        if (SettingToggle.Draw("Allow Multiple Attempts", "##CCMultiAttempts",
                "paying several times the entry cost buys that many turns in a row", ref multiple))
        {
            cc.AllowMultipleAttempts = multiple;
            this.config.Save();
        }
    }

    private void DrawSettingsBody()
    {
        CoinCollectorPreSessionSettingsFields.Draw(this.config);
    }
}
