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
        string message = "Arrows or hjklyubn to move. Esc to quit.";

        while (true)
        {
            Draw(screen, game, message);

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Escape|| (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key is ConsoleKey.C or ConsoleKey.Q))
            {
                return;
            }

            (int dx, int dy)? direction = ReadDirection(key);
            if (direction is null)
            {
                continue;
            }

            (int dx, int dy) = direction.Value;
            message = game.TryMove(dx, dy)
                ? "You step forward."
                : "You bump into a wall.";
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
                char tile = game.Map[x, y];
                screen.Put(x, y + 1, tile, TileColor(tile));
            }
        }

        screen.Put(game.Player.X, game.Player.Y + 1, '@', ConsoleColor.White);

        Player player = game.Player;
        string status =
            $"Level: 1  Gold: {player.Gold}  Hp: {player.Hp}({player.MaxHp})  " +
            $"Str: {player.Strength}  Arm: {player.Armor}  Exp: {player.Experience}  Turn: {game.Turn}";

        screen.Write(0, 0, status, ConsoleColor.Gray);
        screen.Write(0, 23, message, ConsoleColor.Gray);

        screen.Present();
    }

    private static ConsoleColor TileColor(char tile) => tile switch
    {
        '#' => ConsoleColor.DarkGray,
        '+' => ConsoleColor.DarkYellow,
        '>' or '<' => ConsoleColor.Yellow,
        _ => ConsoleColor.Gray,
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