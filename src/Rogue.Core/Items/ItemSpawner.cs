namespace Rogue.Core;

public static class ItemSpawner
{
    public static void Populate(Level level, int depth, Random rng)
    {
        foreach (Room room in level.Rooms)
        {
            if (room.ContainsInterior(level.UpStairs.X, level.UpStairs.Y))
            {
                continue;
            }

            if (rng.Next(100) < 50)
            {
                Item gold = Item.Gold(rng.Next(2, 51 + depth * 10), 0, 0);
                Place(level, room, gold, rng);
            }

            if (rng.Next(100) < 35)
            {
                Place(level, room, CreateItem(rng), rng);
            }
        }
    }

    private static Item CreateItem(Random rng)
    {
        int roll = rng.Next(100);

        if (roll < 40)
        {
            return Item.Potion(0, 0);
        }

        if (roll < 60)
        {
            return Item.Scroll(0, 0);
        }

        return Item.Food(0, 0);
    }

    private static void Place(Level level, Room room, Item item, Random rng)
    {
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

            item.X = x;
            item.Y = y;
            level.AddItem(item);
            return;
        }
    }
}
