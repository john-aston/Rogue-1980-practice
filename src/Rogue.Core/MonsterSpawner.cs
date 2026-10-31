namespace Rogue.Core;

/// <summary>给一层地牢放怪：每个房间 60% 概率一只，起始房间不放。</summary>
public static class MonsterSpawner
{
    public static void Populate(Level level, int depth, Random rng)
    {
        foreach (Room room in level.Rooms)
        {
            if (room.ContainsInterior(level.UpStairs.X, level.UpStairs.Y))
            {
                continue;
            }

            if (rng.Next(100) >= 60)
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

                if (level.MonsterAt(x, y) is not null)
                {
                    continue;
                }

                MonsterDefinition definition = MonsterTable.Pick(depth, rng);
                int hp = Dice.Roll(definition.HpDice, definition.HpSides, rng);
                level.AddMonster(new Monster(definition, x, y, hp));
                break;
            }
        }
    }
}