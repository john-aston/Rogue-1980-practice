namespace Rogue.Core;

public sealed partial class Game
{
    private void PickUpItemOnFloor()
    {
        Item? item = Map.ItemAt(Player.X, Player.Y);

        if (item is null)
        {
            return;
        }

        switch (item.Kind)
        {
            case ItemKind.Gold:
                Map.RemoveItem(item);
                Player.Gold += item.GoldAmount;
                AddMessage($"You pick up {item.GoldAmount} gold.", MessageTone.Good);
                break;

            case ItemKind.Potion:
                if (Player.Potions >= Player.PotionLimit)
                {
                    AddMessage("Your potion belt is full.");
                    return;
                }

                Map.RemoveItem(item);
                Player.Potions++;
                AddMessage("You pick up a healing potion.", MessageTone.Good);
                break;

            case ItemKind.Scroll:
                if (Player.Scrolls >= Player.ScrollLimit)
                {
                    AddMessage("Your scroll case is full.");
                    return;
                }

                Map.RemoveItem(item);
                Player.Scrolls++;
                AddMessage("You pick up a teleport scroll.", MessageTone.Good);
                break;

            case ItemKind.Food:
                if (Player.Rations >= Player.RationLimit)
                {
                    AddMessage("Your pack is full of food.");
                    return;
                }

                Map.RemoveItem(item);
                Player.Rations++;
                AddMessage("You pick up a food ration.", MessageTone.Good);
                break;

            case ItemKind.Amulet:
                Map.RemoveItem(item);
                Player.HasAmulet = true;
                AddMessage("You pick up the Amulet of Yendor!", MessageTone.Good);
                break;
        }
    }

    private void TeleportPlayer()
    {
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int x = _rng.Next(Map.Width);
            int y = _rng.Next(Map.Height);

            if (!Map.IsWalkable(x, y) || Map.MonsterAt(x, y) is not null)
            {
                continue;
            }

            Player.X = x;
            Player.Y = y;
            Sight.Update(Player.X, Player.Y);
            return;
        }
    }

        private void MovePlayerTo((int X, int Y) position)
    {
        Player.X = position.X;
        Player.Y = position.Y;
        Sight.Update(Player.X, Player.Y);
    }

    private DungeonLevel LoadLevel(int number)
    {
        if (_levels.TryGetValue(number, out DungeonLevel? level))
        {
            return level;
        }

        Level map = LevelGenerator.Generate(MapWidth, MapHeight, LevelSeed(number));

        if (number == MaxDepth && map.DownStairs.X >= 0)
        {
            map.Set(map.DownStairs.X, map.DownStairs.Y, Tiles.Floor);
            map.DownStairs = (-1, -1);
        }

        var spawnRng = new Random(LevelSeed(number) ^ 0x2f6e2b1);
        MonsterSpawner.Populate(map, number, spawnRng);

        var itemRng = new Random(LevelSeed(number) ^ 0x51ed270b);
        ItemSpawner.Populate(map, number, itemRng);

        if (number == MaxDepth)
        {
            PlaceAmulet(map, new Random(LevelSeed(number) ^ 0x3c6ef372));
        }

        var state = new DungeonLevel(map);
        _levels[number] = state;
        return state;
    }

    private static void PlaceAmulet(Level level, Random rng)
    {
        foreach (Room room in level.Rooms)
        {
            if (room.ContainsInterior(level.UpStairs.X, level.UpStairs.Y))
            {
                continue;
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                int x = rng.Next(room.X + 1, room.Right);
                int y = rng.Next(room.Y + 1, room.Bottom);

                if (!level.IsWalkable(x, y) || level.ItemAt(x, y) is not null || level.MonsterAt(x, y) is not null)
                {
                    continue;
                }

                level.AddItem(Item.Amulet(x, y));
                return;
            }
        }
    }
}