namespace Rogue.Core;

public static class Dice
{
    public static int Roll(int count, int sides, Random rng)
    {
        int total = 0;

        for (int i = 0; i < count; i++)
        {
            total += rng.Next(1, sides + 1);
        }

        return total;
    }
}