using Rogue.Core;

namespace Rogue.Cli;

internal static class Program
{
    private static void Main()
    {
        if (Console.IsInputRedirected)
        {
            Console.WriteLine("Rogue needs an interactive terminal (stdin is redirected).");
            return;
        }

        if (TerminalTooSmall())
        {
            Console.WriteLine($"Please make the terminal at least {Screen.Width} x {Screen.Height} characters.");
            return;
        }

        Console.CursorVisible = false;
        Console.Clear();
        Console.CancelKeyPress += (_, _) => RestoreTerminal();

        try
        {
            Run();
        }
        finally
        {
            RestoreTerminal();
        }
    }

    private static void Run()
    {
        var screen = new Screen();
        var game = new Game();
        string message = "Move: arrows / hjklyubn. Search: s. Stairs: < >. Quit: Esc.";

        while (true)
        {
            Draw(screen, game, message);

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Escape
                || (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key is ConsoleKey.C or ConsoleKey.Q))
            {
                return;
            }

            if (key.KeyChar is 's' or 'S')
            {
                message = game.Search()
                    ? "You found a secret door!"
                    : "You search but find nothing.";
                continue;
            }

            if (key.KeyChar == '>')
            {
                message = game.TryDescend()
                    ? $"You descend to level {game.LevelNumber}."
                    : "You can't go down here.";
                continue;
            }

            if (key.KeyChar == '<')
            {
                message = game.TryAscend()
                    ? $"You climb to level {game.LevelNumber}."
                    : "You can't go up here.";
                continue;
            }

            (int dx, int dy)? direction = ReadDirection(key);
            if (direction is null)
            {
                continue;
            }

            (int dx, int dy) = direction.Value;

            if (game.TryMove(dx, dy))
            {
                message = game.Map[game.Player.X, game.Player.Y] switch
                {
                    Tiles.StairsDown => "You see a staircase leading down.",
                    Tiles.StairsUp => "You see a staircase leading up.",
                    _ => "You step forward.",
                };
            }
            else
            {
                message = "You bump into a wall.";
            }
        }
    }

    private static void Draw(Screen screen, Game game, string message)
    {
        screen.Clear();

        // 地图画在 1..22 行，第 0 行是状态，第 23 行是消息
        for (int y = 0; y < game.Map.Height; y++)
        {
            for (int x = 0; x < game.Map.Width; x++)
            {
                if (!game.Sight.IsSeen(x, y))
                {
                    continue;   // 没见过的地方保持空白
                }

                char tile = game.Map[x, y];
                bool visible = game.Sight.IsVisible(x, y);

                screen.Put(x, y + 1, tile, visible ? TileColor(tile) : DimColor(tile));
            }
        }

        screen.Put(game.Player.X, game.Player.Y + 1, '@', ConsoleColor.White);

        Player player = game.Player;
        string status =
            $"Level: {game.LevelNumber}  Gold: {player.Gold}  Hp: {player.Hp}({player.MaxHp})  " +
            $"Str: {player.Strength}  Arm: {player.Armor}  Exp: {player.Experience}  Turn: {game.Turn}";

        screen.Write(0, 0, status, ConsoleColor.Gray);
        screen.Write(0, 23, message, ConsoleColor.Gray);

        screen.Present();
    }

    private static ConsoleColor TileColor(char tile) => tile switch
    {
        Tiles.WallHorizontal or Tiles.WallVertical => ConsoleColor.Gray,
        Tiles.Floor => ConsoleColor.Gray,
        Tiles.Corridor => ConsoleColor.DarkGray,
        Tiles.Door => ConsoleColor.DarkYellow,
        Tiles.StairsDown or Tiles.StairsUp => ConsoleColor.Yellow,
        _ => ConsoleColor.Gray,
    };

    /// <summary>记忆里的东西用暗色画。</summary>
    private static ConsoleColor DimColor(char tile) => tile switch
    {
        Tiles.Door or Tiles.StairsDown or Tiles.StairsUp => ConsoleColor.DarkYellow,
        _ => ConsoleColor.DarkGray,
    };

    private static (int dx, int dy)? ReadDirection(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow: return (-1, 0);
            case ConsoleKey.RightArrow: return (1, 0);
            case ConsoleKey.UpArrow: return (0, -1);
            case ConsoleKey.DownArrow: return (0, 1);
        }

        return char.ToLowerInvariant(key.KeyChar) switch
        {
            'h' => (-1, 0),
            'l' => (1, 0),
            'k' => (0, -1),
            'j' => (0, 1),
            'y' => (-1, -1),
            'u' => (1, -1),
            'b' => (-1, 1),
            'n' => (1, 1),
            _ => null,
        };
    }

    private static bool TerminalTooSmall()
    {
        try
        {
            return Console.WindowWidth < Screen.Width || Console.WindowHeight < Screen.Height;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static void RestoreTerminal()
    {
        Console.ResetColor();
        Console.CursorVisible = true;
        Console.Clear();
    }
}