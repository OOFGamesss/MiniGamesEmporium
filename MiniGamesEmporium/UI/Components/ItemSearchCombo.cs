using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Searchable combo box for picking an in-game item by name, backed by the Item Excel sheet.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class ItemSearchCombo
{
    private static Dictionary<uint, string>? _itemById;
    private static List<(uint Id, string Name)>? _allSorted;
    private static List<(uint Id, string Name)> _filtered = new();
    private static string _search = string.Empty;
    private static bool _wasOpen;

    public static bool Draw(string id, ref uint selectedItemId, ref string selectedItemName)
    {
        EnsureLoaded();
        var preview = selectedItemId == 0 ? "(None)" : _itemById!.GetValueOrDefault(selectedItemId, "(Unknown)");
        if (!ImGui.BeginCombo(id, preview))
        {
            _wasOpen = false;
            return false;
        }
        if (!_wasOpen)
        {
            ImGui.SetKeyboardFocusHere();
            _wasOpen = true;
        }

        var changed = false;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText("##ItemSearchInput", ref _search, 128))
            RefreshFilter();

        if (ImGui.Selectable("(None)", selectedItemId == 0))
        {
            selectedItemId   = 0;
            selectedItemName = string.Empty;
            changed          = true;
        }
        ImGui.Separator();
        foreach (var (itemId, name) in _filtered)
        {
            if (!ImGui.Selectable($"{name}##{itemId}", selectedItemId == itemId)) continue;
            selectedItemId   = itemId;
            selectedItemName = name;
            changed          = true;
        }

        ImGui.EndCombo();
        return changed;
    }

    private static void EnsureLoaded()
    {
        if (_allSorted != null) return;
        _itemById  = new Dictionary<uint, string>();
        _allSorted = new List<(uint, string)>();
        var sheet = MiniGamesEmporium.DataManager.GetExcelSheet<Item>();
        foreach (var row in sheet)
        {
            if (row.RowId == 0) continue;
            var name = row.Name.ToString();
            if (string.IsNullOrEmpty(name)) continue;
            _itemById[row.RowId] = name;
            _allSorted.Add((row.RowId, name));
        }
        _allSorted.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        RefreshFilter();
    }

    private static void RefreshFilter()
    {
        _filtered = string.IsNullOrWhiteSpace(_search)
            ? _allSorted!.Take(200).ToList()
            : _allSorted!.Where(x => x.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)).Take(200).ToList();
    }
}
