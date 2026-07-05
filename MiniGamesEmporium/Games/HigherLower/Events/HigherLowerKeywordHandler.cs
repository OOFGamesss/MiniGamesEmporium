using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using MiniGamesEmporium.Services;

/// <summary>No-op keyword handler for Higher/Lower, which is party-based.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Events;
public sealed class HigherLowerKeywordHandler : IChatKeywordHandler
{
    public void TryHandleKeyword(SeString? sender, string message, XivChatType kind) { }
}
