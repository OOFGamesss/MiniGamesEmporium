/// <summary>Implemented by each game that needs to process random-number roll events from chat.</summary>

namespace MiniGamesEmporium.Events;
public interface IChatRollHandler
{
    void TryHandleRoll(string playerName, int rollValue, int rollMax);
}
