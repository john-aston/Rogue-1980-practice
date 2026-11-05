namespace Rogue.Core;

public partial class Game
{
        public bool TryMove(int dx, int dy)
    {
        _messages.Clear();

        int nextX = Player.X + dx;
        int nextY = Player.Y + dy;

        Monster? monster = Map.MonsterAt(nextX, nextY);

        if (monster is not null)
        {
            AttackMonster(monster);
            EndTurn();
            return true;
        }

        if (!Map.IsWalkable(nextX, nextY))
        {
            AddMessage("You bump into a wall.");
            return false;
        }

        Player.X = nextX;
        Player.Y = nextY;
        Sight.Update(Player.X, Player.Y);
        PickUpItemOnFloor();
        EndTurn();
        return true;
    }

    public void Wait()
    {
        _messages.Clear();
        AddMessage("You wait.");
        EndTurn();
    }

    public bool Quaff()
    {
        _messages.Clear();

        if (Player.Potions <= 0)
        {
            AddMessage("You have no potions.", MessageTone.Bad);
            return false;
        }

        Player.Potions--;
        int healed = Dice.Roll(2, 8, _rng);
        Player.Hp = Math.Min(Player.MaxHp, Player.Hp + healed);
        AddMessage($"You drink a healing potion and recover {healed} HP.", MessageTone.Good);
        EndTurn();
        return true;
    }

    public bool Read()
    {
        _messages.Clear();

        if (Player.Scrolls <= 0)
        {
            AddMessage("You have no scrolls.", MessageTone.Bad);
            return false;
        }

        Player.Scrolls--;
        TeleportPlayer();
        AddMessage("You read a teleport scroll. The world lurches.", MessageTone.Good);
        EndTurn();
        return true;
    }

    public bool Eat()
    {
        _messages.Clear();

        if (Player.Rations <= 0)
        {
            AddMessage("You have no food.", MessageTone.Bad);
            return false;
        }

        Player.Rations--;
        Player.Food += 500;

        if (Player.Food > 300)
        {
            _hungryShown = false;
            _weakShown = false;
        }
        else if (Player.Food > 0)
        {
            _weakShown = false;
        }

        AddMessage("You eat a food ration.", MessageTone.Good);
        EndTurn();
        return true;
    }


    public bool UseStairs()
    {
        char tile = Map[Player.X, Player.Y];

        if (tile == Tiles.StairsUp)
        {
            return TryAscend();
        }

        if (tile == Tiles.StairsDown)
        {
            return TryDescend();
        }

        Notify("You are not standing on a staircase.");
        return false;
    }

    public bool TryDescend()
    {
        _messages.Clear();

        if (Map[Player.X, Player.Y] != Tiles.StairsDown || LevelNumber >= MaxDepth)
        {
            AddMessage("You can't go down here.");
            return false;
        }

        LevelNumber++;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.UpStairs);
        AddMessage($"You descend to level {LevelNumber}.");
        return true;
    }

    public bool TryAscend()
    {
        _messages.Clear();

        if (Map[Player.X, Player.Y] != Tiles.StairsUp)
        {
            AddMessage("You can't go up here.");
            return false;
        }

        if (LevelNumber <= 1)
        {
            if (Player.HasAmulet)
            {
                IsWon = true;
                AddMessage("You escape the Dungeon of Doom with the Amulet of Yendor!", MessageTone.Good);
            }
            else
            {
                AddMessage("You can't go up here.");
            }

            return false;
        }

        LevelNumber--;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.DownStairs);
        AddMessage($"You climb to level {LevelNumber}.");
        return true;
    }
}