namespace Rogue.Core;

public sealed class Game
{
    public const int MapWidth = 80;
    public const int MapHeight = 22;
    public const int MaxDepth = 26;
    public const int WakeDistance = 8;
    public const int InventorySize = 26;

    private readonly Random _rng;
    private readonly int _seed;
    private readonly Dictionary<int, DungeonLevel> _levels = new();
    private readonly List<string> _messages = new();
    private DungeonLevel _current;

    public Player Player { get; }
    public ItemKnowledge Knowledge { get; }
    public int Turn { get; private set; }
    public int LevelNumber { get; private set; }
    public bool IsDead => Player.Hp <= 0;

    public Level Map => _current.Map;
    public Visibility Sight => _current.Sight;
    public string Message => _messages.Count == 0 ? "" : _messages[^1];

    public Game() : this(Random.Shared.Next())
    {
    }

    public Game(int seed)
    {
        _seed = seed;
        _rng = new Random(seed);
        Knowledge = new ItemKnowledge(_rng);
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
        Knowledge = new ItemKnowledge(_rng);
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

        Item? here = Map.ItemAt(Player.X, Player.Y);

        if (here is { Kind: ItemKind.Gold })
        {
            Map.RemoveItem(here);
            Player.Gold += here.GoldAmount;
            AddMessage($"You pick up {here.GoldAmount} gold.");
        }

        EndTurn();
        return true;
    }

    public void Wait()
    {
        _messages.Clear();
        AddMessage("You wait.");
        EndTurn();
    }

    public bool PickUp()
    {
        _messages.Clear();

        Item? item = Map.ItemAt(Player.X, Player.Y);

        if (item is null)
        {
            AddMessage("There is nothing here.");
            return false;
        }

        if (item.Kind == ItemKind.Gold)
        {
            Map.RemoveItem(item);
            Player.Gold += item.GoldAmount;
            AddMessage($"You pick up {item.GoldAmount} gold.");
            EndTurn();
            return true;
        }

        if (Player.Inventory.Count >= InventorySize)
        {
            AddMessage("Your pack is full.");
            return false;
        }

        Map.RemoveItem(item);
        Player.Inventory.Add(item);
        AddMessage($"You pick up {item.DisplayName(Knowledge)}.");
        EndTurn();
        return true;
    }

    public bool Drop(int index)
    {
        _messages.Clear();

        if (index < 0 || index >= Player.Inventory.Count)
        {
            return false;
        }

        if (Map.ItemAt(Player.X, Player.Y) is not null)
        {
            AddMessage("You can't drop it here.");
            return false;
        }

        Item item = Player.Inventory[index];
        Player.Inventory.RemoveAt(index);
        item.X = Player.X;
        item.Y = Player.Y;
        Map.AddItem(item);
        AddMessage($"You drop {item.DisplayName(Knowledge)}.");
        EndTurn();
        return true;
    }

    public bool Quaff(int index)
    {
        _messages.Clear();

        if (index < 0 || index >= Player.Inventory.Count)
        {
            return false;
        }

        Item item = Player.Inventory[index];

        if (item.Kind != ItemKind.Potion)
        {
            AddMessage("You can't quaff that.");
            return false;
        }

        Player.Inventory.RemoveAt(index);
        AddMessage($"You quaff {item.DisplayName(Knowledge)}.");
        Knowledge.Identify(item.Potion);
        ApplyPotion(item.Potion);
        EndTurn();
        return true;
    }

    public bool Read(int index)
    {
        _messages.Clear();

        if (index < 0 || index >= Player.Inventory.Count)
        {
            return false;
        }

        Item item = Player.Inventory[index];

        if (item.Kind != ItemKind.Scroll)
        {
            AddMessage("You can't read that.");
            return false;
        }

        Player.Inventory.RemoveAt(index);
        AddMessage($"You read {item.DisplayName(Knowledge)}.");
        Knowledge.Identify(item.Scroll);

        switch (item.Scroll)
        {
            case ScrollType.Identify:
                IdentifySomething();
                break;

            case ScrollType.Teleport:
                TeleportPlayer();
                AddMessage("You feel a wrenching sensation.");
                break;

            case ScrollType.MagicMapping:
                Sight.RevealAll();
                AddMessage("A map appears in your mind.");
                break;
        }

        EndTurn();
        return true;
    }

    public bool Eat(int index)
    {
        _messages.Clear();

        if (index < 0 || index >= Player.Inventory.Count)
        {
            return false;
        }

        Item item = Player.Inventory[index];

        if (item.Kind != ItemKind.Food)
        {
            AddMessage("You can't eat that.");
            return false;
        }

        Player.Inventory.RemoveAt(index);
        Player.Food += item.Nutrition;
        AddMessage("You eat the food.");
        EndTurn();
        return true;
    }

    public bool Wield(int index)
    {
        _messages.Clear();

        if (index < 0 || index >= Player.Inventory.Count)
        {
            return false;
        }

        Item item = Player.Inventory[index];

        if (item.Kind != ItemKind.Weapon)
        {
            AddMessage("You can't wield that.");
            return false;
        }

        Player.Inventory.RemoveAt(index);

        if (Player.Weapon is { } oldWeapon)
        {
            Player.Inventory.Add(oldWeapon);
        }

        Player.Weapon = item;
        AddMessage($"You wield {item.Name}.");
        EndTurn();
        return true;
    }

    public bool Wear(int index)
    {
        _messages.Clear();

        if (index < 0 || index >= Player.Inventory.Count)
        {
            return false;
        }

        Item item = Player.Inventory[index];

        if (item.Kind != ItemKind.Armor)
        {
            AddMessage("You can't wear that.");
            return false;
        }

        if (Player.WornArmor is not null)
        {
            AddMessage("You are already wearing armor.");
            return false;
        }

        Player.Inventory.RemoveAt(index);
        Player.WornArmor = item;
        Player.Armor += item.Armor;
        AddMessage($"You wear {item.Name}.");
        EndTurn();
        return true;
    }

    public bool TakeOff()
    {
        _messages.Clear();

        if (Player.WornArmor is not { } armor)
        {
            AddMessage("You aren't wearing armor.");
            return false;
        }

        if (Player.Inventory.Count >= InventorySize)
        {
            AddMessage("Your pack is full.");
            return false;
        }

        Player.Inventory.Add(armor);
        Player.Armor -= armor.Armor;
        Player.WornArmor = null;
        AddMessage($"You take off the {armor.Name}.");
        EndTurn();
        return true;
    }

    public void Notify(string message)
    {
        _messages.Clear();
        AddMessage(message);
    }

    public bool Search()
    {
        _messages.Clear();
        bool found = false;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int x = Player.X + dx;
                int y = Player.Y + dy;

                if (!Map.IsSecretDoor(x, y))
                {
                    continue;
                }

                if (_rng.Next(5) != 0)
                {
                    continue;
                }

                Map.RevealSecretDoor(x, y);
                found = true;
            }
        }

        AddMessage(found ? "You found a secret door!" : "You search but find nothing.");
        EndTurn();
        return found;
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

        if (Map[Player.X, Player.Y] != Tiles.StairsUp || LevelNumber <= 1)
        {
            AddMessage("You can't go up here.");
            return false;
        }

        LevelNumber--;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.DownStairs);
        AddMessage($"You climb to level {LevelNumber}.");
        return true;
    }

    private void ApplyPotion(PotionType type)
    {
        switch (type)
        {
            case PotionType.Healing:
                Player.Hp = Math.Min(Player.MaxHp, Player.Hp + Dice.Roll(2, 8, _rng));
                AddMessage("You feel better.");
                break;

            case PotionType.ExtraHealing:
                Player.Hp = Math.Min(Player.MaxHp, Player.Hp + Dice.Roll(4, 8, _rng));
                AddMessage("You feel much better.");
                break;

            case PotionType.Poison:
                Player.Hp -= Dice.Roll(1, 6, _rng);
                AddMessage("You feel very sick.");
                break;

            case PotionType.Experience:
                AddMessage("You feel more experienced.");
                GainExperience(10);
                break;
        }
    }

    private void IdentifySomething()
    {
        foreach (Item item in Player.Inventory)
        {
            if (item.Kind == ItemKind.Potion && !Knowledge.IsKnown(item.Potion))
            {
                Knowledge.Identify(item.Potion);
                AddMessage($"You now know what {item.DisplayName(Knowledge)} is.");
                return;
            }

            if (item.Kind == ItemKind.Scroll && !Knowledge.IsKnown(item.Scroll))
            {
                Knowledge.Identify(item.Scroll);
                AddMessage($"You now know what {item.DisplayName(Knowledge)} is.");
                return;
            }
        }

        AddMessage("You have nothing to identify.");
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
        MonstersAct();

        if (IsDead)
        {
            AddMessage($"You die on level {LevelNumber}.");
        }
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

        int roll = _rng.Next(1, 21) + Player.Level;
        int target = 10 + definition.Armor;

        if (roll < target)
        {
            AddMessage($"You miss the {definition.Name}.");
            return;
        }

        int damage = Player.Weapon is { } weapon
            ? Dice.Roll(weapon.DamageDice, weapon.DamageSides, _rng)
            : Dice.Roll(1, 4, _rng);

        damage += Math.Max(0, (Player.Strength - 16) / 2);
        monster.Hp -= damage;

        if (monster.Hp > 0)
        {
            AddMessage($"You hit the {definition.Name}.");
            return;
        }

        Map.RemoveMonster(monster);
        AddMessage($"You defeat the {definition.Name}.");
        GainExperience(definition.Experience);
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
        AddMessage($"The {definition.Name} hits you for {damage}.");
    }

    private void GainExperience(int amount)
    {
        Player.Experience += amount;

        while (Player.Experience >= Player.Level * 5)
        {
            Player.Level++;
            Player.MaxHp += 3;
            Player.Hp = Player.MaxHp;
            AddMessage($"Welcome to level {Player.Level}.");
        }
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

    private void AddMessage(string message) => _messages.Add(message);

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
        ItemSpawner.Populate(map, number, Knowledge, itemRng);

        var state = new DungeonLevel(map);
        _levels[number] = state;
        return state;
    }

    private int LevelSeed(int number) => unchecked(_seed + number * 7919);
}