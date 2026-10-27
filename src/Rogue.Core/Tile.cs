namespace Rogue.Core;

/// <summary>地图字符常量 + 可走判定</summary>
public static class Tiles
{
    public const char Rock = ' ';
    public const char Floor = '.';
    public const char Corridor = '#';
    public const char WallHorizontal = '-'; //水平
    public const char WallVertical = '|';   //竖直
    public const char Door = '+';
    public const char StairsDown = '>';
    public const char StairsUp = '<';
    //可走判定部份
    public static bool IsWalkable(char tile)
        => tile is Floor or Corridor or Door or StairsDown or StairsUp;

}