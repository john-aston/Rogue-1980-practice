namespace Rogue.Core;

/// <summary>怪物模板。数值是按原版量级估的，后面可以对着 monsters.c 校准。</summary>
public sealed record MonsterDefinition(
    char Symbol,
    string Name,
    int Level,
    int HpDice,
    int HpSides,
    int DamageDice,
    int DamageSides,
    int Armor,
    int Experience);