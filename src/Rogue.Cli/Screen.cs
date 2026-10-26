//self-made 80×24 帧缓冲 + ANSI 差分输出
using System.Text;

namespace Rogue.Cli;

public sealed class Screen
{
    public const int Width = 80;
    public const int Height = 24;

    private readonly char[,] _chars = new char[Height, Width];
    private readonly ConsoleColor[,] _colors = new ConsoleColor[Height,Width];
    private readonly char[,] _shownChars = new char[Height, Width];
    private readonly ConsoleColor[,] _shownColors = new ConsoleColor[Height, Width];

    public Screen()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _shownChars[y, x] = '\0';
            }
        }
    }

    public void Clear()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _chars[y, x] = ' ';
                _colors[y, x] = ConsoleColor.Gray;//可设置的16颜色之一
            }
        }
    }

    public void Put(int x, int y, char c, ConsoleColor color)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return;
        }

        _chars[y, x] = c;
        _colors[y, x] = color;
    }

     /// <summary>从 (x, y) 开始写一串字符，超出右边界就截断。</summary>
    public void Write(int x, int y, string text, ConsoleColor color)
    {
        for (int i = 0; i < text.Length && x + i < Width; i++)
        {
            Put(x + i, y, text[i], color);
        }
    }

    /// <summary>把这一帧和上一帧的差异刷到终端。</summary>
    public void Present()
    {
        var output = new StringBuilder(4096);
        int cursorX = -1;
        int cursorY = -1;
        int lastColor = -1;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char c = _chars[y, x];
                ConsoleColor color = _colors[y, x];

                if (c == _shownChars[y, x] && color == _shownColors[y, x])
                {
                    continue;
                }

                // 光标不在目标位置才需要发定位序列
                if (x != cursorX || y != cursorY)
                {
                    output.Append("\x1b[").Append(y + 1).Append(';').Append(x + 1).Append('H');
                    cursorX = x;
                    cursorY = y;
                }

                if ((int)color != lastColor)
                {
                    output.Append("\x1b[").Append(ToAnsi(color)).Append('m');                    
                    lastColor = (int)color;
                }

                output.Append(c);

                _shownChars[y, x] = c;
                _shownColors[y, x] = color;
                cursorX++;
            }
        }

        if (output.Length > 0)
        {
            output.Append("\x1b[0m");
            Console.Write(output.ToString());
        }
    }

    /// <summary>ConsoleColor 到 ANSI 前景色的映射，别直接拿枚举值当 ANSI 码用。</summary>
    private static int ToAnsi(ConsoleColor color) => color switch
    {
        ConsoleColor.Black => 30,
        ConsoleColor.DarkRed => 31,
        ConsoleColor.DarkGreen => 32,
        ConsoleColor.DarkYellow => 33,
        ConsoleColor.DarkBlue => 34,
        ConsoleColor.DarkMagenta => 35,
        ConsoleColor.DarkCyan => 36,
        ConsoleColor.Gray => 37,
        ConsoleColor.DarkGray => 90,
        ConsoleColor.Red => 91,
        ConsoleColor.Green => 92,
        ConsoleColor.Yellow => 93,
        ConsoleColor.Blue => 94,
        ConsoleColor.Magenta => 95,
        ConsoleColor.Cyan => 96,
        ConsoleColor.White => 97,
        _ => 37,
    };

}