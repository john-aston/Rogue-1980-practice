namespace Rogue.Core;

//一个矩形的房间，坐标包含四周的墙
public sealed class Room
{
    public Room(int x, int y, int width, int height, bool isDark)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        IsDark = isDark;
    }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsDark { get; }

    //只读属性
    public int Right => X + Width - 1;
    public int Bottom => Y + Height - 1;
    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;

    //整个矩形，包括墙
    public bool Contains(int x, int y)
        => x >= X && x <= Right && y >= Y && y <= Bottom;

    //只算能站人的内部地面
    public bool ContainsInterior(int x, int y)
        => x > X && x < Right && y > Y && y < Bottom;
}