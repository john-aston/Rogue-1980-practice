namespace Rogue.Core;

/// <summary>玩家状态。没有经验/等级，成长只靠金币升级。</summary>
public sealed class Player
{
    public const int BaseArmor = 4;
    public const int ChainMailArmor = 4;
    public const int PotionLimit = 6;
    public const int ScrollLimit = 9;
    public const int RationLimit = 9;

    public int X { get; set; }
    public int Y { get; set; }

    public int Hp { get; set; } = 12;
    public int MaxHp { get; set; } = 12;

    /// <summary>基础 4 + 链甲 4，升级后再加。</summary>
    public int Armor { get; set; } = BaseArmor + ChainMailArmor;

    public int Gold { get; set; }
    public int Food { get; set; } = 1300;

    public int Potions { get; set; }
    public int Scrolls { get; set; }
    public int Rations { get; set; }

    public int SwordLevel { get; set; }
    public int ArmorLevel { get; set; }
    public bool HasAmulet { get; set; }
}
