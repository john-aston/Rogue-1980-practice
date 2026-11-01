// 玩家状态（位置 + 状态栏数值）

namespace Rogue.Core;

public sealed class Player
{
    //玩家当前的坐标
    public int X { get; set; }
    public int Y { get; set; }

    public int Hp { get; set; } = 12;   //get读取，set写入
    public int MaxHp { get; set; } = 12;
    public int Level { get; set; } = 1;
    public int Strength { get; set; } = 16;
    public int Armor { get; set; } = 4;
    public int Gold { get; set; }
    public int Experience { get; set; }
    
    //背包、当前武器、当前护甲
    public List<Item> Inventory { get; } = new();
    public Item? Weapon { get; set; }
    public Item? WornArmor { get; set; }

    //饥饿值
    public int Food { get; set; } = 1300;

    //背包里是否有 `,`；通关判定用；amulet是通关的关键道具
    public bool HasAmulet
    {
        get
        {
            foreach (Item item in Inventory)
            {
                if (item.Kind == ItemKind.Amulet)
                {
                    return true;
                }
            }

            return false;
        }
    }


}