/// <summary>Pure bracket sizing maths for Deathroll Tournament seeding.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Utility;
public static class BracketMath
{
    public static int NextPowerOf2(int n)
    {
        if (n <= 1) return 2;
        var p = 1;
        while (p < n) p <<= 1;
        return p;
    }
}
