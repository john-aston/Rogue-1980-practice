namespace Rogue.Core;

public static class TrapSpawner
{
    public static void Populate(Level level, int depth, Random rng)
    {
        foreach (Room room in level.Rooms)
        {
            if (room.ContainsInterior(level.UpStairs.X, level.UpStairs.Y))
            {
                continue;
            }

            if (rng.Next(100) >= 30)
            {
                continue;
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                int x = rng.Next(room.X + 1, room.Right);
                int y = rng.Next(room.Y + 1, room.Bottom);

                if (!level.IsWalkable(x, y))
                {
                    continue;
                }

                if (level[x, y] is Tiles.StairsUp or Tiles.StairsDown)
                {
                    continue;
                }

                if (level.ItemAt(x, y) is not null || level.MonsterAt(x, y) is not null)
                {
                    continue;
                }

                if (level.TrapAt(x, y) is not null)
                {
                    continue;
                }

                level.AddTrap(new Trap((TrapType)rng.Next(4), x, y));
                break;
            }
        }
    }
}
