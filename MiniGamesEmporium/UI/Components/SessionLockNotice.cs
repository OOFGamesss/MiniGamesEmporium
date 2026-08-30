using Dalamud.Bindings.ImGui;

/// <summary>Draws the shared warning shown when settings are locked by an active session.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class SessionLockNotice
{
    public static void Draw(string message)
    {
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X);
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, message);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
    }
}
