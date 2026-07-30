using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

/// <summary>Lays icon buttons out left to right, wrapping to a new line when the row runs out of width.</summary>

namespace MiniGamesEmporium.UI.Components;
public sealed class ShoutButtonRow
{
    private float spaceLeft = -1f;

    public bool Button(FontAwesomeIcon icon, string label, string id)
    {
        var width   = UIHelper.CalcButtonSize(icon, label).X;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        if (this.spaceLeft < 0f)
            this.spaceLeft = ImGui.GetContentRegionAvail().X;
        else if (width + spacing <= this.spaceLeft)
        {
            ImGui.SameLine();
            this.spaceLeft -= spacing;
        }
        else
        {
            this.spaceLeft = ImGui.GetContentRegionAvail().X;
        }
        this.spaceLeft -= width;
        return UIHelper.IconTextButton(icon, label, id);
    }
}
