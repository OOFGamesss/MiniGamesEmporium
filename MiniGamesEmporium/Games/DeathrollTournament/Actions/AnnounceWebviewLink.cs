using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the live web spectator link so players can sign up or watch the bracket in a browser.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceWebviewLink
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        if (string.IsNullOrWhiteSpace(config.DeathrollTournament.WebSpectatorUrl)) return;
        var msg = DeathrollMessageFormatter.Format(config.DeathrollTournament.Chat.AnnounceWebviewMessage, config);
        chatQueue.Enqueue(msg);
    }
}
