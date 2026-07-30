using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.UI.Components;

/// <summary>Renders the prize type selector (Gil pot / Item / Custom text) shared by the start door and settings tab.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Components;
public static class DeathrollPrizeFields
{
    public static void Draw(PluginConfiguration config, string imguiSuffix, float fieldWidth)
    {
        var cfg = config.DeathrollTournament;
        ImGui.TextDisabled("Prize");
        ImGui.Spacing();

        var prizeType = cfg.PrizeType;
        var changed   = false;
        if (ImGui.RadioButton($"Gil Pot##DRPrizeGil_{imguiSuffix}", prizeType == DeathrollPrizeType.Gil))
        {
            prizeType = DeathrollPrizeType.Gil;
            changed   = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton($"Item##DRPrizeItem_{imguiSuffix}", prizeType == DeathrollPrizeType.Item))
        {
            prizeType = DeathrollPrizeType.Item;
            changed   = true;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton($"Custom##DRPrizeCustom_{imguiSuffix}", prizeType == DeathrollPrizeType.Custom))
        {
            prizeType = DeathrollPrizeType.Custom;
            changed   = true;
        }
        if (changed)
        {
            cfg.PrizeType = prizeType;
            config.Save();
        }

        switch (prizeType)
        {
            case DeathrollPrizeType.Item:
                ImGui.Spacing();
                DrawItemField(config, imguiSuffix, fieldWidth);
                break;
            case DeathrollPrizeType.Custom:
                ImGui.Spacing();
                DrawCustomTextField(config, imguiSuffix, fieldWidth);
                break;
        }
    }

    private static void DrawItemField(PluginConfiguration config, string imguiSuffix, float fieldWidth)
    {
        var cfg  = config.DeathrollTournament;
        var id   = cfg.PrizeItemId;
        var name = cfg.PrizeItemName;
        ImGui.TextDisabled("Prize item");
        ImGui.SetNextItemWidth(fieldWidth);
        if (!ItemSearchCombo.Draw($"##DRPrizeItemCombo_{imguiSuffix}", ref id, ref name)) return;
        cfg.PrizeItemId   = id;
        cfg.PrizeItemName = name;
        config.Save();
    }

    private static void DrawCustomTextField(PluginConfiguration config, string imguiSuffix, float fieldWidth)
    {
        var cfg  = config.DeathrollTournament;
        var text = cfg.PrizeCustomText;
        ImGui.TextDisabled("Prize (max 64 characters)");
        ImGui.SetNextItemWidth(fieldWidth);
        if (!ImGui.InputText($"##DRPrizeCustomText_{imguiSuffix}", ref text, 64)) return;
        cfg.PrizeCustomText = text;
        config.Save();
    }
}
