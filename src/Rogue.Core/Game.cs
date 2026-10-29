namespace Rogue.Core;

public sealed class Game
{
    public const int MapWidth = 80;
    public const int MapHeight = 22;
    public const int MaxDepth = 26;

    private readonly Random _rng;
    private readonly int _seed;
    private readonly Dictionary<int, DungeonLevel> _levels = new();
    private DungeonLevel _current;

    public Player Player { get; }
    public int Turn { get; private set; }
    public int LevelNumber { get; private set; }

    public Level Map => _current.Map;
    public Visibility Sight => _current.Sight;

    public Game() : this(Random.Shared.Next())
    {
    }

    public Game(int seed)
    {
        _seed = seed;
        _rng = new Random(seed);
        Player = new Player();

        LevelNumber = 1;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.UpStairs);
    }

    /// <summary>给测试用：直接给地图和起点，不跑生成器。</summary>
    public Game(Level map, int playerX, int playerY)
    {
        _seed = 0;
        _rng = new Random(0);
        Player = new Player { X = playerX, Y = playerY };

        LevelNumber = 1;
        _current = new DungeonLevel(map);
        _levels[LevelNumber] = _current;
        _current.Sight.Update(playerX, playerY);
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

    /// <summary>站在 &gt; 上时下到下一层。</summary>
    public bool TryDescend()
    {
        if (Map[Player.X, Player.Y] != Tiles.StairsDown)
        {
            return false;
        }

        if (LevelNumber >= MaxDepth)
        {
            return false;
        }

        LevelNumber++;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.UpStairs);
        return true;
    }

    /// <summary>站在 &lt; 上时回到上一层。第 1 层没有出口。</summary>
    public bool TryAscend()
    {
        if (Map[Player.X, Player.Y] != Tiles.StairsUp)
        {
            return false;
        }

        if (LevelNumber <= 1)
        {
            return false;   // 还没拿到 Amulet
        }

        LevelNumber--;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.DownStairs);
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

    private void MovePlayerTo((int X, int Y) position)
    {
        Player.X = position.X;
        Player.Y = position.Y;
        Sight.Update(Player.X, Player.Y);
    }

    /// <summary>第一次进入某层时生成它，之后一直复用同一份地图与视野记忆。</summary>
    private DungeonLevel LoadLevel(int number)
    {
        if (_levels.TryGetValue(number, out DungeonLevel? level))
        {
            return level;
        }

        Level map = LevelGenerator.Generate(MapWidth, MapHeight, LevelSeed(number));

        // 最底层没有向下的楼梯
        if (number == MaxDepth && map.DownStairs.X >= 0)
        {
            map.Set(map.DownStairs.X, map.DownStairs.Y, Tiles.Floor);
            map.DownStairs = (-1, -1);
        }

        var state = new DungeonLevel(map);
        _levels[number] = state;
        return state;
    }

    /// <summary>每层的种子由本局种子推出来，同样的局号每层布局固定。</summary>
    private int LevelSeed(int number) => unchecked(_seed + number * 7919);
}