namespace Rogue.Core;

/// <summary>地图字符常量 + 可走判定</summary>
//所有地图字符的常量，避免到处写字符本身，而是用名词来表达
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
    
    //可走判定部份，在特定的字符上面才能行走
    public static bool IsWalkable(char tile)
        => tile is Floor or Corridor or Door or StairsDown or StairsUp;

}