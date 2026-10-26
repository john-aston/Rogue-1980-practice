//地图数据、碰撞、挖房间和走廊，代码顺序仍比较混乱，建议日后按照类型进行分类，统一格式

namespace Rogue.Core;

/// <summary>一层地牢。字符网格，x 是列，y 是行。</summary>
public sealed class Level //sealed class 不能被继承
{
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

    public bool InBounds(int x, int y) //确认座标在范围内
    {
        if (x < 0 || y < 0) return false;
        if (x >= Width || y >= Height) return false;
        return true;
    }

    public bool IsWalkable(int x, int y)
    {
        if (!InBounds(x, y)) 
        return false;
        
        char c = this[x, y];
        
        return c == '.' || c == '>' || c == '<' || c == '+';
    }

    /// <summary>把一块矩形区域挖成房间地面。</summary>
    public void CarveRoom(int x, int y, int width, int height)
    {
        for (int row = y; row < y + height; row++)
        {
            for (int col = x; col < x + width; col++)
            {
                Set(col, row, '.');
            }
        }
    }

    /// <summary>先横后竖的 L 形走廊</summary>
    public void CarveCorridor(int x1, int y1, int x2, int y2)
    {
        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        {
            Set(x, y1, '.');
        }

        for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
        {
            Set(x2, y, '.');
        }
    }


    private readonly char[,] _tiles; //二维数组，存储地图字符

    public Level(int width, int height)
    {
        _tiles = new char[height, width]; //创建二维数组，行数为 height，列数为 width
        Fill('#'); 
    }

    public int Width => _tiles.GetLength(1);
    public int Height => _tiles.GetLength(0);

    /// <summary>读格子。调用方需保证坐标在界内。</summary>
    public char this[int x,int y] =>_tiles[y,x];

    public void Set(int x, int y , char tile)
    {
        if (InBounds(x, y))
        {
            _tiles[y,x] = tile;
        }
    }
    
    //临时关卡
     public static Level CreateDemo()
    {
        var level = new Level(80, 22);
        level.CarveRoom(2, 2, 14, 6);
        level.CarveRoom(40, 12, 18, 7);
        level.CarveCorridor(16, 5, 39, 14);
        level.Set(14, 6, '>');
        return level;
    }


}