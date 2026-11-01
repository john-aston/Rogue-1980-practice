namespace Rogue.Core;

/// <summary>存档的根对象。只放数据，不放行为；由 Game 负责快照与恢复。</summary>
public sealed class SaveData
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public int Seed { get; set; }
    public int Turn { get; set; }
    public int LevelNumber { get; set; }
    public PlayerData Player { get; set; } = new();
    public KnowledgeData Knowledge { get; set; } = new();
    public List<LevelData> Levels { get; set; } = new();
}

public sealed class PlayerData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Level { get; set; }
    public int Strength { get; set; }
    public int Armor { get; set; }
    public int Gold { get; set; }
    public int Experience { get; set; }
    public int Food { get; set; }
    public List<ItemData> Inventory { get; set; } = new();
    public ItemData? Weapon { get; set; }
    public ItemData? WornArmor { get; set; }
}

public sealed class KnowledgeData
{
    public string[] PotionAppearances { get; set; } = Array.Empty<string>();
    public string[] ScrollTitles { get; set; } = Array.Empty<string>();
    public bool[] PotionsKnown { get; set; } = Array.Empty<bool>();
    public bool[] ScrollsKnown { get; set; } = Array.Empty<bool>();
}

public sealed class LevelData
{
    public int Number { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string[] Rows { get; set; } = Array.Empty<string>();
    public List<RoomData> Rooms { get; set; } = new();
    public List<SecretDoorData> SecretDoors { get; set; } = new();
    public int UpStairsX { get; set; } = -1;
    public int UpStairsY { get; set; } = -1;
    public int DownStairsX { get; set; } = -1;
    public int DownStairsY { get; set; } = -1;
    public List<MonsterData> Monsters { get; set; } = new();
    public List<ItemData> Items { get; set; } = new();
    public List<TrapData> Traps { get; set; } = new();
    public bool[] Seen { get; set; } = Array.Empty<bool>();
}

public sealed class RoomData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsDark { get; set; }
}

public sealed class SecretDoorData
{
    public int X { get; set; }
    public int Y { get; set; }
    public char Disguise { get; set; }
}

public sealed class MonsterData
{
    public char Symbol { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public bool IsAwake { get; set; }
}

public sealed class TrapData
{
    public TrapType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsFound { get; set; }
}

public sealed class ItemData
{
    public ItemKind Kind { get; set; }
    public char Symbol { get; set; }
    public string Name { get; set; } = "";
    public string Appearance { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public PotionType Potion { get; set; }
    public ScrollType Scroll { get; set; }
    public int Nutrition { get; set; }
    public int DamageDice { get; set; }
    public int DamageSides { get; set; }
    public int Armor { get; set; }
    public int GoldAmount { get; set; }

    public static ItemData From(Item item) => new ItemData
    {
        Kind = item.Kind,
        Symbol = item.Symbol,
        Name = item.Name,
        Appearance = item.Appearance,
        X = item.X,
        Y = item.Y,
        Potion = item.Potion,
        Scroll = item.Scroll,
        Nutrition = item.Nutrition,
        DamageDice = item.DamageDice,
        DamageSides = item.DamageSides,
        Armor = item.Armor,
        GoldAmount = item.GoldAmount,
    };

    public Item ToItem() => new Item(Kind, Symbol, Name, X, Y)
    {
        Appearance = Appearance,
        Potion = Potion,
        Scroll = Scroll,
        Nutrition = Nutrition,
        DamageDice = DamageDice,
        DamageSides = DamageSides,
        Armor = Armor,
        GoldAmount = GoldAmount,
    };
}
