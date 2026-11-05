namespace Rogue.Core;

public sealed partial class Game
{
        private void EndTurn()
    {
        Turn++;
        HungerTick();

        if (!IsDead)
        {
            MonstersAct();
        }

        if (IsDead)
        {
            AddDeathMessage();
        }
    }

    private void HungerTick()
    {
        Player.Food--;

        if (Player.Food <= 300 && !_hungryShown)
        {
            AddMessage("You are hungry.", MessageTone.Bad);
            _hungryShown = true;
        }

        if (Player.Food <= 0 && !_weakShown)
        {
            AddMessage("You are weak.", MessageTone.Bad);
            _weakShown = true;
        }

        if (Player.Food <= -200)
        {
            _deathCause = "starvation";
            AddMessage("You faint from lack of food.", MessageTone.Bad);
            Player.Hp = 0;
        }
    }

    private void AddDeathMessage()
    {
        AddMessage(_deathCause is null
            ? $"You die on level {LevelNumber}."
            : $"You die of {_deathCause}.", MessageTone.Bad);
    }
}