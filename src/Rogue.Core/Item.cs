namespace Rogue.Core;

public enum ItemKind
{
    Potion,
    Scroll,
    Food,
    Gold,
    Amulet,
}

/// <summary>地上的一件物品。没有鉴定、没有外观、没有装备属性。</summary>
public sealed class Item
{
    public Item(ItemKind kind, int x, int y, int goldAmount = 0)
    {
        Kind = kind;
        X = x;
        Y = y;
        GoldAmount = goldAmount;
    }

    public ItemKind Kind { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public int GoldAmount { get; }

    public char Symbol => Kind switch
    {
        ItemKind.Potion => '!',
        ItemKind.Scroll => '?',
        ItemKind.Food => '%',
        ItemKind.Gold => '*',
        ItemKind.Amulet => ',',
        _ => '?',
    };

    public string Name => Kind switch
    {
        ItemKind.Potion => "healing potion",
        ItemKind.Scroll => "teleport scroll",
        ItemKind.Food => "food ration",
        ItemKind.Gold => "gold",
        ItemKind.Amulet => "Amulet of Yendor",
        _ => "item",
    };

    public static Item Potion(int x, int y) => new(ItemKind.Potion, x, y);
    public static Item Scroll(int x, int y) => new(ItemKind.Scroll, x, y);
    public static Item Food(int x, int y) => new(ItemKind.Food, x, y);
    public static Item Gold(int amount, int x, int y) => new(ItemKind.Gold, x, y, amount);
    public static Item Amulet(int x, int y) => new(ItemKind.Amulet, x, y);
}
