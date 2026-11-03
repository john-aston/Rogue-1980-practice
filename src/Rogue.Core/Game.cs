namespace Rogue.Core;

public enum MessageTone
{
    Normal,
    Good,
    Bad,
}

public sealed class Game
{
    public const int MapWidth = 80;
    public const int MapHeight = 22;
    public const int MaxDepth = 26;
    public const int WakeDistance = 8;
    public const int MaxUpgradeLevel = 5;
    public const int UpgradeCostBase = 40;

    private readonly Random _rng;
    private readonly int _seed;
    private readonly Dictionary<int, DungeonLevel> _levels = new();
    private readonly List<(string Text, MessageTone Tone)> _messages = new();
    private bool _hungryShown;
    private bool _weakShown;
    private string? _deathCause;
    private DungeonLevel _current;

    public Player Player { get; }
    public int Turn { get; private set; }
    public int LevelNumber { get; private set; }
    public bool IsDead => Player.Hp <= 0;
    public bool IsWon { get; private set; }

    public int Score => Player.Gold + LevelNumber * 10 + (Player.HasAmulet ? 1000 : 0);

    public Level Map => _current.Map;
    public Visibility Sight => _current.Sight;
    public string Message => _messages.Count == 0 ? "" : _messages[^1].Text;
    public MessageTone Tone => _messages.Count == 0 ? MessageTone.Normal : _messages[^1].Tone;

    public Game() : this(Random.Shared.Next())
    {
    }

    public Game(int seed)
    {
        _seed = seed;
        _rng = new Random(seed);
        Player = new Player();

        LevelNumber = 1;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.UpStairs);
    }

    /// <summary>给测试用：直接给地图和起点，不跑生成器。</summary>
    public Game(Level map, int playerX, int playerY)
    {
        _seed = 0;
        _rng = new Random(0);
        Player = new Player { X = playerX, Y = playerY };

        LevelNumber = 1;
        _current = new DungeonLevel(map);
        _levels[LevelNumber] = _current;
        _current.Sight.Update(playerX, playerY);
    }

    public bool TryMove(int dx, int dy)
    {
        _messages.Clear();

        int nextX = Player.X + dx;
        int nextY = Player.Y + dy;

        Monster? monster = Map.MonsterAt(nextX, nextY);

        if (monster is not null)
        {
            AttackMonster(monster);
            EndTurn();
            return true;
        }

        if (!Map.IsWalkable(nextX, nextY))
        {
            AddMessage("You bump into a wall.");
            return false;
        }

        Player.X = nextX;
        Player.Y = nextY;
        Sight.Update(Player.X, Player.Y);
        PickUpItemOnFloor();
        EndTurn();
        return true;
    }

    public void Wait()
    {
        _messages.Clear();
        AddMessage("You wait.");
        EndTurn();
    }

    public bool Quaff()
    {
        _messages.Clear();

        if (Player.Potions <= 0)
        {
            AddMessage("You have no potions.", MessageTone.Bad);
            return false;
        }

        Player.Potions--;
        int healed = Dice.Roll(2, 8, _rng);
        Player.Hp = Math.Min(Player.MaxHp, Player.Hp + healed);
        AddMessage($"You drink a healing potion and recover {healed} HP.", MessageTone.Good);
        EndTurn();
        return true;
    }

    public bool Read()
    {
        _messages.Clear();

        if (Player.Scrolls <= 0)
        {
            AddMessage("You have no scrolls.", MessageTone.Bad);
            return false;
        }

        Player.Scrolls--;
        TeleportPlayer();
        AddMessage("You read a teleport scroll. The world lurches.", MessageTone.Good);
        EndTurn();
        return true;
    }

    public bool Eat()
    {
        _messages.Clear();

        if (Player.Rations <= 0)
        {
            AddMessage("You have no food.", MessageTone.Bad);
            return false;
        }

        Player.Rations--;
        Player.Food += 500;

        if (Player.Food > 300)
        {
            _hungryShown = false;
            _weakShown = false;
        }
        else if (Player.Food > 0)
        {
            _weakShown = false;
        }

        AddMessage("You eat a food ration.", MessageTone.Good);
        EndTurn();
        return true;
    }

    /// <summary>金币升级。1 剑、2 甲、3 药水、4 食物、5 最大 HP。</summary>
    public bool Buy(int option)
    {
        _messages.Clear();

        return option switch
        {
            1 => BuySword(),
            2 => BuyArmor(),
            3 => BuyPotion(),
            4 => BuyRation(),
            5 => BuyMaxHp(),
            _ => BuyNothing(),
        };
    }

    public bool UseStairs()
    {
        char tile = Map[Player.X, Player.Y];

        if (tile == Tiles.StairsUp)
        {
            return TryAscend();
        }

        if (tile == Tiles.StairsDown)
        {
            return TryDescend();
        }

        Notify("You are not standing on a staircase.");
        return false;
    }

    public bool TryDescend()
    {
        _messages.Clear();

        if (Map[Player.X, Player.Y] != Tiles.StairsDown || LevelNumber >= MaxDepth)
        {
            AddMessage("You can't go down here.");
            return false;
        }

        LevelNumber++;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.UpStairs);
        AddMessage($"You descend to level {LevelNumber}.");
        return true;
    }

    public bool TryAscend()
    {
        _messages.Clear();

        if (Map[Player.X, Player.Y] != Tiles.StairsUp)
        {
            AddMessage("You can't go up here.");
            return false;
        }

        if (LevelNumber <= 1)
        {
            if (Player.HasAmulet)
            {
                IsWon = true;
                AddMessage("You escape the Dungeon of Doom with the Amulet of Yendor!", MessageTone.Good);
            }
            else
            {
                AddMessage("You can't go up here.");
            }

            return false;
        }

        LevelNumber--;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.DownStairs);
        AddMessage($"You climb to level {LevelNumber}.");
        return true;
    }

    public void Notify(string message)
    {
        _messages.Clear();
        AddMessage(message);
    }

    private bool BuySword()
    {
        if (Player.SwordLevel >= MaxUpgradeLevel)
        {
            AddMessage("Your sword is already at its maximum.");
            return false;
        }

        int cost = UpgradeCostBase * (Player.SwordLevel + 1);

        if (Player.Gold < cost)
        {
            AddMessage($"You need {cost} gold for that.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= cost;
        Player.SwordLevel++;
        AddMessage($"Your sword is now +{Player.SwordLevel}. Gold left: {Player.Gold}.", MessageTone.Good);
        return true;
    }

    private bool BuyArmor()
    {
        if (Player.ArmorLevel >= MaxUpgradeLevel)
        {
            AddMessage("Your armor is already at its maximum.");
            return false;
        }

        int cost = UpgradeCostBase * (Player.ArmorLevel + 1);

        if (Player.Gold < cost)
        {
            AddMessage($"You need {cost} gold for that.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= cost;
        Player.ArmorLevel++;
        Player.Armor++;
        AddMessage($"Your armor is now +{Player.ArmorLevel}. Gold left: {Player.Gold}.", MessageTone.Good);
        return true;
    }

    private bool BuyPotion()
    {
        if (Player.Potions >= Player.PotionLimit)
        {
            AddMessage("Your potion belt is full.");
            return false;
        }

        if (Player.Gold < 25)
        {
            AddMessage("You need 25 gold for a potion.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= 25;
        Player.Potions++;
        AddMessage($"You buy a healing potion. Gold left: {Player.Gold}.", MessageTone.Good);
        return true;
    }

    private bool BuyRation()
    {
        if (Player.Rations >= Player.RationLimit)
        {
            AddMessage("Your pack is full of food.");
            return false;
        }

        if (Player.Gold < 15)
        {
            AddMessage("You need 15 gold for food.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= 15;
        Player.Rations++;
        AddMessage($"You buy a food ration. Gold left: {Player.Gold}.", MessageTone.Good);
        return true;
    }

    private bool BuyMaxHp()
    {
        if (Player.Gold < 60)
        {
            AddMessage("You need 60 gold for that.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= 60;
        Player.MaxHp += 2;
        Player.Hp += 2;
        AddMessage($"Your maximum HP is now {Player.MaxHp}.", MessageTone.Good);
        return true;
    }

    private bool BuyNothing()
    {
        AddMessage("There is no such upgrade.");
        return false;
    }

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

    private void EndTurn()
    {
        Turn++;
        HungerTick();

        if (!IsDead)
        {
            MonstersAct();
        }

        if (IsDead)
        {
            AddDeathMessage();
        }
    }

    private void HungerTick()
    {
        Player.Food--;

        if (Player.Food <= 300 && !_hungryShown)
        {
            AddMessage("You are hungry.", MessageTone.Bad);
            _hungryShown = true;
        }

        if (Player.Food <= 0 && !_weakShown)
        {
            AddMessage("You are weak.", MessageTone.Bad);
            _weakShown = true;
        }

        if (Player.Food <= -200)
        {
            _deathCause = "starvation";
            AddMessage("You faint from lack of food.", MessageTone.Bad);
            Player.Hp = 0;
        }
    }

    private void AddDeathMessage()
    {
        AddMessage(_deathCause is null
            ? $"You die on level {LevelNumber}."
            : $"You die of {_deathCause}.", MessageTone.Bad);
    }

    private void MonstersAct()
    {
        foreach (Monster monster in Map.Monsters)
        {
            if (IsDead)
            {
                return;
            }

            if (monster.IsDead)
            {
                continue;
            }

            if (!monster.IsAwake)
            {
                if (Distance(monster.X, monster.Y, Player.X, Player.Y) > WakeDistance)
                {
                    continue;
                }

                monster.IsAwake = true;
            }

            if (IsAdjacent(monster.X, monster.Y, Player.X, Player.Y))
            {
                MonsterAttack(monster);
                continue;
            }

            StepTowardPlayer(monster);
        }
    }

    private void AttackMonster(Monster monster)
    {
        MonsterDefinition definition = monster.Definition;

        int roll = _rng.Next(1, 21) + 2 + Player.SwordLevel;
        int target = 10 + definition.Armor;

        if (roll < target)
        {
            AddMessage($"You miss the {definition.Name}.");
            return;
        }

        int damage = Dice.Roll(2, 4, _rng) + Player.SwordLevel;
        monster.Hp -= damage;

        if (monster.Hp > 0)
        {
            AddMessage($"You hit the {definition.Name} for {damage}.");
            return;
        }

        Map.RemoveMonster(monster);
        int reward = 4 + definition.Level * 3;
        Player.Gold += reward;
        AddMessage($"You defeat the {definition.Name} and find {reward} gold.", MessageTone.Good);
    }

    private void MonsterAttack(Monster monster)
    {
        MonsterDefinition definition = monster.Definition;

        int roll = _rng.Next(1, 21) + definition.Level;
        int target = 10 + Player.Armor;

        if (roll < target)
        {
            AddMessage($"The {definition.Name} misses you.");
            return;
        }

        int damage = Dice.Roll(definition.DamageDice, definition.DamageSides, _rng);
        Player.Hp -= damage;
        AddMessage($"The {definition.Name} hits you for {damage}.", MessageTone.Bad);
    }

    private void StepTowardPlayer(Monster monster)
    {
        int dx = Math.Sign(Player.X - monster.X);
        int dy = Math.Sign(Player.Y - monster.Y);

        if (TryStep(monster, dx, dy))
        {
            return;
        }

        if (TryStep(monster, dx, 0))
        {
            return;
        }

        TryStep(monster, 0, dy);
    }

    private bool TryStep(Monster monster, int dx, int dy)
    {
        if (dx == 0 && dy == 0)
        {
            return false;
        }

        int x = monster.X + dx;
        int y = monster.Y + dy;

        if (!Map.IsWalkable(x, y))
        {
            return false;
        }

        if (Map.MonsterAt(x, y) is not null)
        {
            return false;
        }

        if (x == Player.X && y == Player.Y)
        {
            return false;
        }

        monster.X = x;
        monster.Y = y;
        return true;
    }

    private static bool IsAdjacent(int x, int y, int targetX, int targetY)
        => Math.Abs(x - targetX) <= 1
        && Math.Abs(y - targetY) <= 1
        && (x != targetX || y != targetY);

    private static int Distance(int x, int y, int targetX, int targetY)
        => Math.Max(Math.Abs(x - targetX), Math.Abs(y - targetY));

    private void AddMessage(string text, MessageTone tone = MessageTone.Normal)
        => _messages.Add((text, tone));

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
