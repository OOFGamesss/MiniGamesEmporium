using Dalamud.Game.Text.SeStringHandling;

/// <summary>Implemented by each game that needs to react to keyword messages in enabled chat channels.</summary>

namespace MiniGamesEmporium.Events;
public interface IChatKeywordHandler
{
    void TryHandleKeyword(SeString? sender, string message);
}
