using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.State;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Draws the Transaction History tab, displaying a scrollable, resizable ledger of all recorded Gil trades with columns for player name, game, amount, and local date and time.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class TransactionHistoryTab
{
    private readonly PluginConfiguration config;
    public TransactionHistoryTab(PluginConfiguration config)
    {
        this.config = config;
    }
    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Transaction ledger");
        ImGui.Spacing();
        ImGui.TextDisabled($"Total transactions: {this.config.Transactions.Count}");
        ImGui.Separator();
        ImGui.Spacing();
        using var table = ImRaii.Table(
            "##TransactionTable",
            4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable,
            new Vector2(0, -1));
        if (!table.Success) return;
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Game", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Amount (Gil)", ImGuiTableColumnFlags.WidthFixed, 130f);
        ImGui.TableSetupColumn("Date / Time (Local)", ImGuiTableColumnFlags.WidthFixed, 160f);
        ImGui.TableHeadersRow();
        for (var i = this.config.Transactions.Count - 1; i >= 0; i--)
        {
            DrawTransactionRow(this.config.Transactions[i]);
        }
    }
    private static void DrawTransactionRow(TransactionRecord record)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(record.PlayerName);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(record.GameName);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextUnformatted($"{record.Amount:N0}");
        ImGui.TableSetColumnIndex(3);
        ImGui.TextUnformatted(record.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
    }
}
