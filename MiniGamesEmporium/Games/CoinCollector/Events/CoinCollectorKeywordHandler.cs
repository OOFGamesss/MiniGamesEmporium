using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Services;

/// <summary>No-op keyword handler for Coin Collector, which is party-based.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Events;
public sealed class CoinCollectorKeywordHandler : IChatKeywordHandler
{
    public string GameName => CoinCollectorGameIds.DisplayName;

    public void TryHandleKeyword(SeString? sender, string message, XivChatType kind) { }
}
