namespace Rogue.Core;

//掷骰子，本地生成随机数，掷 `count` 个 `sides` 面骰并求和；战斗、HP、陷阱都用它

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