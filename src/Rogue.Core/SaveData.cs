namespace Rogue.Core;

/// <summary>存档根对象。版本 2：简化后的物品/金币系统。</summary>
public sealed class SaveData
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public int Seed { get; set; }
    public int Turn { get; set; }
    public int LevelNumber { get; set; }
    public PlayerData Player { get; set; } = new();
    public List<LevelData> Levels { get; set; } = new();
}

public sealed class PlayerData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Armor { get; set; }
    public int Gold { get; set; }
    public int Food { get; set; }
    public int Potions { get; set; }
    public int Scrolls { get; set; }
    public int Rations { get; set; }
    public int SwordLevel { get; set; }
    public int ArmorLevel { get; set; }
    public bool HasAmulet { get; set; }
}

public sealed class LevelData
{
    public int Number { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string[] Rows { get; set; } = Array.Empty<string>();
    public List<RoomData> Rooms { get; set; } = new();
    public int UpStairsX { get; set; } = -1;
    public int UpStairsY { get; set; } = -1;
    public int DownStairsX { get; set; } = -1;
    public int DownStairsY { get; set; } = -1;
    public List<MonsterData> Monsters { get; set; } = new();
    public List<ItemData> Items { get; set; } = new();
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

public sealed class MonsterData
{
    public char Symbol { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public bool IsAwake { get; set; }
}

public sealed class ItemData
{
    public ItemKind Kind { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int GoldAmount { get; set; }

    public static ItemData From(Item item) => new ItemData
    {
        Kind = item.Kind,
        X = item.X,
        Y = item.Y,
        GoldAmount = item.GoldAmount,
    };

    public Item ToItem() => new Item(Kind, X, Y, GoldAmount);
}
