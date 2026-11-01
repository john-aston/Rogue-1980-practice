namespace Rogue.Core;
/// <summary>
/// 这部分是用来存放物品信息的
/// </summary>

//药水/卷轴/食物/武器/护甲/金币/Amulet
public enum ItemKind
{
    Potion,
    Scroll,
    Food,
    Weapon,
    Armor,
    Gold,
    Amulet,
}

public enum PotionType
{
    Healing,
    ExtraHealing,
    Poison,
    Experience,
}
//鉴定/传送/魔法地图
public enum ScrollType
{
    Identify,
    Teleport,
    MagicMapping,
}
//符号、真名、坐标
public sealed class Item
{
    public Item(ItemKind kind, char symbol, string name, int x, int y)
    {
        Kind = kind;
        Symbol = symbol;
        Name = name;
        X = x;
        Y = y;
    }

    public ItemKind Kind { get; }
    public char Symbol { get; }
    public string Name { get; }
    public string Appearance { get; init; } = "";
    public int X { get; set; }
    public int Y { get; set; }

    public PotionType Potion { get; init; }
    public ScrollType Scroll { get; init; }
    public int Nutrition { get; init; }
    public int DamageDice { get; init; }
    public int DamageSides { get; init; }
    public int Armor { get; init; }
    public int GoldAmount { get; init; }

    public string DisplayName(ItemKnowledge knowledge) => Kind switch
    {
        ItemKind.Potion when !knowledge.IsKnown(Potion) => Appearance,
        ItemKind.Scroll when !knowledge.IsKnown(Scroll) => Appearance,
        _ => Name,
    };

    public static Item CreatePotion(PotionType type, ItemKnowledge knowledge, int x, int y)
        => new Item(ItemKind.Potion, '!', PotionName(type), x, y)
        {
            Potion = type,
            Appearance = $"a {knowledge.PotionAppearance(type)} potion",
        };

    public static Item CreateScroll(ScrollType type, ItemKnowledge knowledge, int x, int y)
        => new Item(ItemKind.Scroll, '?', ScrollName(type), x, y)
        {
            Scroll = type,
            Appearance = $"a scroll titled {knowledge.ScrollTitle(type)}",
        };

    public static Item CreateFood(int x, int y)
        => new Item(ItemKind.Food, '%', "food ration", x, y) { Nutrition = 500 };

    public static Item CreateGold(int amount, int x, int y)
        => new Item(ItemKind.Gold, '*', "gold", x, y) { GoldAmount = amount };

    public static Item CreateAmulet(int x, int y)
        => new Item(ItemKind.Amulet, ',', "Amulet of Yendor", x, y);

    public static string PotionName(PotionType type) => type switch
    {
        PotionType.Healing => "potion of healing",
        PotionType.ExtraHealing => "potion of extra healing",
        PotionType.Poison => "potion of poison",
        PotionType.Experience => "potion of experience",
        _ => "potion",
    };

    public static string ScrollName(ScrollType type) => type switch
    {
        ScrollType.Identify => "scroll of identify",
        ScrollType.Teleport => "scroll of teleportation",
        ScrollType.MagicMapping => "scroll of magic mapping",
        _ => "scroll",
    };
}