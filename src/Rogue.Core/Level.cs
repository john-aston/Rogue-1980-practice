namespace Rogue.Core;

//绘制地牢的规则，提供画布和画笔
public sealed class Level
{
    //外部碰不到、引用本身也不许换，只能在构造函数里 new 一次。
    private readonly char[,] _tiles;
    private readonly List<Room> _rooms = new();
    private readonly HashSet<(int X, int Y)> _secretDoors = new();

    //建一张新地图，先全部填成“石头”，当白纸用。
    public Level(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new char[height, width];
        Fill(Tiles.Rock);
    }

    //地图尺寸建好后固定
    public int Width { get; }   //对外公开一个建好后就不能改的宽度值，只能读取Level.Width，不能改Level._width
    public int Height { get; }  //同上

    public IReadOnlyList<Room> Rooms => _rooms;
    public IReadOnlySet<(int X, int Y)> SecretDoors => _secretDoors;

    /// <summary>楼梯位置；还没放楼梯时是 (-1, -1)。</summary>
    public (int X, int Y) UpStairs { get; set; } = (-1, -1);
    public (int X, int Y) DownStairs { get; set; } = (-1, -1);

    public char this[int x, int y] => _tiles[y, x];

    //边界检查
    public bool InBounds(int x, int y)
        => x >= 0 && y >= 0 && x < Width && y < Height;

    //可走检查
    public bool IsWalkable(int x, int y)
        => InBounds(x, y) && Tiles.IsWalkable(this[x, y]);

    public void Fill(char tile)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _tiles[y, x] = tile;
            }
        }
    }

    public void Set(int x, int y, char tile)
    {
        if (InBounds(x, y))
        {
            _tiles[y, x] = tile;
        }
    }

    /// <summary>只把岩石挖成走廊，不覆盖已有的门、地面和楼梯。</summary>
    public void CarveCorridor(int x, int y)
    {
        if (InBounds(x, y) && _tiles[y, x] == Tiles.Rock)
        {
            _tiles[y, x] = Tiles.Corridor;
        }
    }

    //画一个带墙的房间
    public void DrawRoom(int x, int y, int width, int height)
    {
        for (int col = x; col < x + width; col++)
        {
            Set(col, y, Tiles.WallHorizontal);
            Set(col, y + height - 1, Tiles.WallHorizontal);
        }

        for (int row = y + 1; row < y + height - 1; row++)
        {
            Set(x, row, Tiles.WallVertical);
            Set(x + width - 1, row, Tiles.WallVertical);

            for (int col = x + 1; col < x + width - 1; col++)
            {
                Set(col, row, Tiles.Floor);
            }
        }
    }

    public void AddRoom(Room room) => _rooms.Add(room);

    /// <summary>玩家站在哪个房间的内部；走廊和门口返回 null。</summary>
    public Room? RoomAt(int x, int y)
    {
        foreach (Room room in _rooms)
        {
            if (room.ContainsInterior(x, y))
            {
                return room;
            }
        }

        return null;
    }

    public bool IsSecretDoor(int x, int y) => _secretDoors.Contains((x, y));

    /// <summary>把一扇门伪装成墙，并记下它的真实身份。</summary>
    public void AddSecretDoor(int x, int y, char disguise)
    {
        Set(x, y, disguise);
        _secretDoors.Add((x, y));
    }

    /// <summary>揭开秘密门。返回是否真的揭开了一扇。</summary>
    public bool RevealSecretDoor(int x, int y)
    {
        if (!_secretDoors.Remove((x, y)))
        {
            return false;
        }

        Set(x, y, Tiles.Door);
        return true;
    }

    /// <summary>用字符画测试地图，主要给测试用。</summary>
    public static Level FromAscii(params string[] rows)
    {
        var level = new Level(rows[0].Length, rows.Length);

        for (int y = 0; y < rows.Length; y++)
        {
            for (int x = 0; x < rows[y].Length; x++)
            {
                level.Set(x, y, rows[y][x]);
            }
        }

        return level;
    }
}