using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Draws the Transaction History tab listing all recorded Gil trades.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class TransactionHistoryTab
{
    private const int PageSize = 100;
    private readonly HistoryService historyService;
    private int currentPage;

    public TransactionHistoryTab(HistoryService historyService)
    {
        this.historyService = historyService;
    }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Transaction ledger");
        ImGui.Spacing();
        var cached = this.historyService.CachedTransactions;
        ImGui.TextDisabled($"Total transactions: {cached.Count}");
        ImGui.Separator();
        ImGui.Spacing();
        var totalPages     = Math.Max(1, (cached.Count + PageSize - 1) / PageSize);
        if (this.currentPage >= totalPages) this.currentPage = totalPages - 1;
        var startIndex     = cached.Count - 1 - this.currentPage * PageSize;
        var endIndex       = Math.Max(0, cached.Count - (this.currentPage + 1) * PageSize);
        var showPagination = totalPages > 1;
        var tableHeight    = ImGui.GetContentRegionAvail().Y - (showPagination ? ImGui.GetFrameHeightWithSpacing() : 0f);
        var tableRendered  = false;
        using (var table = ImRaii.Table(
            "##TransactionTable",
            4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable,
            new Vector2(0, tableHeight)))
        {
            tableRendered = table.Success;
            if (table.Success)
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Game", ImGuiTableColumnFlags.WidthFixed, 100f);
                ImGui.TableSetupColumn("Amount (Gil)", ImGuiTableColumnFlags.WidthFixed, 130f);
                ImGui.TableSetupColumn("Date / Time (Local)", ImGuiTableColumnFlags.WidthFixed, 160f);
                ImGui.TableHeadersRow();
                for (var i = startIndex; i >= endIndex; i--)
                    DrawTransactionRow(cached[i]);
            }
        }
        if (showPagination && tableRendered)
            this.currentPage = UIHelper.DrawPagination(this.currentPage, totalPages, "Transaction");
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
