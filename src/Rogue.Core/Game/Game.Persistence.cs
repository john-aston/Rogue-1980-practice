    namespace Rogue.Core;

    public partial class Game{

    public SaveData Snapshot()
    {
        var data = new SaveData
        {
            Seed = _seed,
            Turn = Turn,
            LevelNumber = LevelNumber,
            Player = new PlayerData
            {
                X = Player.X,
                Y = Player.Y,
                Hp = Player.Hp,
                MaxHp = Player.MaxHp,
                Armor = Player.Armor,
                Gold = Player.Gold,
                Food = Player.Food,
                Potions = Player.Potions,
                Scrolls = Player.Scrolls,
                Rations = Player.Rations,
                SwordLevel = Player.SwordLevel,
                ArmorLevel = Player.ArmorLevel,
                HasAmulet = Player.HasAmulet,
            },
        };

        foreach (int number in _levels.Keys.OrderBy(key => key))
        {
            data.Levels.Add(SnapshotLevel(number, _levels[number]));
        }

        return data;
    }

    public static Game Restore(SaveData data)
    {
        if (data.Version != SaveData.CurrentVersion)
        {
            throw new InvalidOperationException("Unsupported save version.");
        }

        return new Game(data);
    }

    private Game(SaveData data)
    {
        _seed = data.Seed;
        _rng = new Random(unchecked(data.Seed ^ (data.Turn * 7919)));

        Player = new Player
        {
            X = data.Player.X,
            Y = data.Player.Y,
            Hp = data.Player.Hp,
            MaxHp = data.Player.MaxHp,
            Armor = data.Player.Armor,
            Gold = data.Player.Gold,
            Food = data.Player.Food,
            Potions = data.Player.Potions,
            Scrolls = data.Player.Scrolls,
            Rations = data.Player.Rations,
            SwordLevel = data.Player.SwordLevel,
            ArmorLevel = data.Player.ArmorLevel,
            HasAmulet = data.Player.HasAmulet,
        };

        foreach (LevelData level in data.Levels)
        {
            _levels[level.Number] = BuildLevel(level);
        }

        if (!_levels.TryGetValue(data.LevelNumber, out DungeonLevel? current))
        {
            throw new InvalidOperationException("Save file is missing the current level.");
        }

        LevelNumber = data.LevelNumber;
        Turn = data.Turn;
        _current = current;
        _current.Sight.Update(Player.X, Player.Y);

        _hungryShown = Player.Food <= 300;
        _weakShown = Player.Food <= 0;
    }

    private static LevelData SnapshotLevel(int number, DungeonLevel state)
    {
        Level level = state.Map;
        var rows = new string[level.Height];

        for (int y = 0; y < level.Height; y++)
        {
            var chars = new char[level.Width];

            for (int x = 0; x < level.Width; x++)
            {
                chars[x] = level[x, y];
            }

            rows[y] = new string(chars);
        }

        var data = new LevelData
        {
            Number = number,
            Width = level.Width,
            Height = level.Height,
            Rows = rows,
            UpStairsX = level.UpStairs.X,
            UpStairsY = level.UpStairs.Y,
            DownStairsX = level.DownStairs.X,
            DownStairsY = level.DownStairs.Y,
            Seen = state.Sight.SnapshotSeen(),
        };

        foreach (Room room in level.Rooms)
        {
            data.Rooms.Add(new RoomData
            {
                X = room.X,
                Y = room.Y,
                Width = room.Width,
                Height = room.Height,
                IsDark = room.IsDark,
            });
        }

        foreach (Monster monster in level.Monsters)
        {
            data.Monsters.Add(new MonsterData
            {
                Symbol = monster.Definition.Symbol,
                X = monster.X,
                Y = monster.Y,
                Hp = monster.Hp,
                IsAwake = monster.IsAwake,
            });
        }

        foreach (Item item in level.Items)
        {
            data.Items.Add(ItemData.From(item));
        }

        return data;
    }

    private static DungeonLevel BuildLevel(LevelData data)
    {
        var level = new Level(data.Width, data.Height);

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                level.Set(x, y, data.Rows[y][x]);
            }
        }

        foreach (RoomData room in data.Rooms)
        {
            level.AddRoom(new Room(room.X, room.Y, room.Width, room.Height, room.IsDark));
        }

        level.UpStairs = (data.UpStairsX, data.UpStairsY);
        level.DownStairs = (data.DownStairsX, data.DownStairsY);

        foreach (MonsterData saved in data.Monsters)
        {
            MonsterDefinition? definition = null;

            foreach (MonsterDefinition candidate in MonsterTable.All)
            {
                if (candidate.Symbol == saved.Symbol)
                {
                    definition = candidate;
                    break;
                }
            }

            if (definition is null)
            {
                continue;
            }

            level.AddMonster(new Monster(definition, saved.X, saved.Y, saved.Hp)
            {
                IsAwake = saved.IsAwake,
            });
        }

        foreach (ItemData item in data.Items)
        {
            level.AddItem(item.ToItem());
        }

        var state = new DungeonLevel(level);
        state.Sight.RestoreSeen(data.Seen);
        return state;
    }

    private int LevelSeed(int number) => unchecked(_seed + number * 7919);

    }