// 玩家状态（位置 + 状态栏数值）

namespace Rogue.Core;

public sealed class Player
{
    public int X { get; set; }
    public int Y { get; set; }

    public int Hp { get; set; } = 12;   //get读取，set写入
    public int MaxHp { get; set; } = 12;
    public int Level { get; set; } = 1;
    public int Strength { get; set; } = 16;
    public int Armor { get; set; } = 4;
    public int Gold { get; set; }
    public int Experience { get; set; }

    public List<Item> Inventory { get; } = new();
    public Item? Weapon { get; set; }
    public Item? WornArmor { get; set; }
    public int Food { get; set; } = 500;


}