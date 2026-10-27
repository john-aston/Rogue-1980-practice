namespace Rogue.Core;

public sealed class Game
{
    public const int MapWidth = 80;
    public const int MapHeight = 22;

    private readonly Random _rng;

    public Level Map { get; }
    public Player Player { get; }
    public Visibility Sight { get; }
    public int Turn { get; private set; }
    public int LevelNumber { get; }

    public Game() : this(Random.Shared.Next())
    {
    }

    public Game(int seed)
    {
        Map = LevelGenerator.Generate(MapWidth, MapHeight, seed);
        LevelNumber = 1;
        Player = new Player { X = Map.UpStairs.X, Y = Map.UpStairs.Y };
        Sight = new Visibility(Map);
        Sight.Update(Player.X, Player.Y);
        _rng = new Random(seed);
    }

    /// <summary>给测试用：直接给地图和起点，不跑生成器。</summary>
    public Game(Level map, int playerX, int playerY)
    {
        Map = map;
        LevelNumber = 1;
        Player = new Player { X = playerX, Y = playerY };
        Sight = new Visibility(map);
        Sight.Update(playerX, playerY);
        _rng = new Random(0);
    }

    public bool TryMove(int dx, int dy)
    {
        int nextX = Player.X + dx;
        int nextY = Player.Y + dy;

        if (!Map.IsWalkable(nextX, nextY))
        {
            return false;
        }

        Player.X = nextX;
        Player.Y = nextY;
        Turn++;
        Sight.Update(Player.X, Player.Y);
        return true;
    }

    /// <summary>搜索身边八格。原版是每格五分之一的机会找到秘密门。</summary>
    public bool Search()
    {
        bool found = false;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int x = Player.X + dx;
                int y = Player.Y + dy;

                if (!Map.IsSecretDoor(x, y))
                {
                    continue;
                }

                if (_rng.Next(5) != 0)
                {
                    continue;
                }

                Map.RevealSecretDoor(x, y);
                found = true;
            }
        }

        Turn++;
        return found;
    }
}