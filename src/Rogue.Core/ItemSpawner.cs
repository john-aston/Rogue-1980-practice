namespace Rogue.Core;

public static class ItemSpawner
{
    private static readonly (string Name, int Dice, int Sides)[] Weapons =
    {
        ("dagger", 1, 4),
        ("mace", 1, 6),
        ("long sword", 2, 4),
    };

    private static readonly (string Name, int Armor)[] Armors =
    {
        ("leather armor", 2),
        ("chain mail", 4),
        ("plate mail", 6),
    };

    public static void Populate(Level level, int depth, ItemKnowledge knowledge, Random rng)
    {
        foreach (Room room in level.Rooms)
        {
            if (room.ContainsInterior(level.UpStairs.X, level.UpStairs.Y))
            {
                continue;
            }

            if (rng.Next(100) < 50)
            {
                Item gold = Item.CreateGold(rng.Next(2, 51 + depth * 10), 0, 0);
                Place(level, room, gold, rng);
            }

            if (rng.Next(100) < 35)
            {
                Place(level, room, CreateItem(knowledge, rng), rng);
            }
        }
    }

    private static Item CreateItem(ItemKnowledge knowledge, Random rng)
    {
        int roll = rng.Next(100);

        if (roll < 30)
        {
            return Item.CreatePotion((PotionType)rng.Next(4), knowledge, 0, 0);
        }

        if (roll < 55)
        {
            return Item.CreateScroll((ScrollType)rng.Next(3), knowledge, 0, 0);
        }

        if (roll < 75)
        {
            return Item.CreateFood(0, 0);
        }

        if (roll < 90)
        {
            (string name, int dice, int sides) = Weapons[rng.Next(Weapons.Length)];
            return new Item(ItemKind.Weapon, ')', name, 0, 0)
            {
                DamageDice = dice,
                DamageSides = sides,
            };
        }

        (string armorName, int armor) = Armors[rng.Next(Armors.Length)];
        return new Item(ItemKind.Armor, '[', armorName, 0, 0) { Armor = armor };
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