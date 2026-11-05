namespace Rogue.Core;

public sealed class Monster
{
    public Monster(MonsterDefinition definition, int x, int y, int hp)
    {
        Definition = definition;
        X = x;
        Y = y;
        Hp = hp;
    }

    public MonsterDefinition Definition { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public bool IsAwake { get; set; }
    public bool IsDead => Hp <= 0;
}