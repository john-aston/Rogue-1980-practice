namespace Rogue.Core;

public partial class Game
{
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

        int roll = _rng.Next(1, 21) + 2 + Player.SwordLevel;
        int target = 10 + definition.Armor;

        if (roll < target)
        {
            AddMessage($"You miss the {definition.Name}.");
            return;
        }

        int damage = Dice.Roll(2, 4, _rng) + Player.SwordLevel;
        monster.Hp -= damage;

        if (monster.Hp > 0)
        {
            AddMessage($"You hit the {definition.Name} for {damage}.");
            return;
        }

        Map.RemoveMonster(monster);
        int reward = 4 + definition.Level * 3;
        Player.Gold += reward;
        AddMessage($"You defeat the {definition.Name} and find {reward} gold.", MessageTone.Good);
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
        AddMessage($"The {definition.Name} hits you for {damage}.", MessageTone.Bad);
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
}