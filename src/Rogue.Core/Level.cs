namespace Rogue.Core;

/// <summary>一层地牢：格子、房间、物品、怪物、楼梯。</summary>
public sealed class Level
{
    private readonly char[,] _tiles;
    private readonly List<Room> _rooms = new();
    private readonly List<Item> _items = new();
    private readonly List<Monster> _monsters = new();

    public Level(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new char[height, width];
        Fill(Tiles.Rock);
    }

    public int Width { get; }
    public int Height { get; }

    public IReadOnlyList<Room> Rooms => _rooms;
    public IReadOnlyList<Item> Items => _items;
    public IReadOnlyList<Monster> Monsters => _monsters;

    public (int X, int Y) UpStairs { get; set; } = (-1, -1);
    public (int X, int Y) DownStairs { get; set; } = (-1, -1);

    public char this[int x, int y] => _tiles[y, x];

    public bool InBounds(int x, int y)
        => x >= 0 && y >= 0 && x < Width && y < Height;

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

    /// <summary>画一个带墙的房间，内部填成地面。</summary>
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

    public void AddMonster(Monster monster) => _monsters.Add(monster);

    public void RemoveMonster(Monster monster) => _monsters.Remove(monster);

    public Monster? MonsterAt(int x, int y)
    {
        foreach (Monster monster in _monsters)
        {
            if (monster.X == x && monster.Y == y)
            {
                return monster;
            }
        }

        return null;
    }

    public void AddItem(Item item) => _items.Add(item);

    public void RemoveItem(Item item) => _items.Remove(item);

    public Item? ItemAt(int x, int y)
    {
        foreach (Item item in _items)
        {
            if (item.X == x && item.Y == y)
            {
                return item;
            }
        }

        return null;
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
