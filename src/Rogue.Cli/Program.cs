using Rogue.Core;

namespace Rogue.Cli;

internal static class Program
{
    private const string Hint =
        "Move hjklyubn | g get | i inv | q r e use | w W T equip | d drop | s search | < > stairs | . wait";

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

        while (true)
        {
            Draw(screen, game);

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Escape
                || (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key is ConsoleKey.C or ConsoleKey.Q))
            {
                return;
            }

            if (key.KeyChar is 'i' or 'I')
            {
                ShowInventory(screen, game, "Inventory");
            }
            else if (key.KeyChar is 'g' or 'G')
            {
                game.PickUp();
            }
            else if (key.KeyChar is 'q')
            {
                int? index = ChooseItem(screen, game, "Quaff which item?");
                if (index is int slot)
                {
                    game.Quaff(slot);
                }
            }
            else if (key.KeyChar is 'r')
            {
                int? index = ChooseItem(screen, game, "Read which item?");
                if (index is int slot)
                {
                    game.Read(slot);
                }
            }
            else if (key.KeyChar is 'e')
            {
                int? index = ChooseItem(screen, game, "Eat which item?");
                if (index is int slot)
                {
                    game.Eat(slot);
                }
            }
            else if (key.KeyChar is 'w')
            {
                int? index = ChooseItem(screen, game, "Wield which item?");
                if (index is int slot)
                {
                    game.Wield(slot);
                }
            }
            else if (key.KeyChar is 'W')
            {
                int? index = ChooseItem(screen, game, "Wear which item?");
                if (index is int slot)
                {
                    game.Wear(slot);
                }
            }
            else if (key.KeyChar is 'T')
            {
                game.TakeOff();
            }
            else if (key.KeyChar is 'd')
            {
                int? index = ChooseItem(screen, game, "Drop which item?");
                if (index is int slot)
                {
                    game.Drop(slot);
                }
            }
            else if (key.KeyChar is 's' or 'S')
            {
                game.Search();
            }
            else if (key.KeyChar == '>')
            {
                game.TryDescend();
            }
            else if (key.KeyChar == '<')
            {
                game.TryAscend();
            }
            else if (key.KeyChar == '.')
            {
                game.Wait();
            }
            else
            {
                (int dx, int dy)? direction = ReadDirection(key);
                if (direction is null)
                {
                    continue;
                }

                (int dx, int dy) = direction.Value;
                game.TryMove(dx, dy);
            }

            if (game.IsDead)
            {
                Draw(screen, game);
                screen.Write(0, 23, game.Message, ConsoleColor.Red);
                screen.Present();
                Console.ReadKey(intercept: true);
                return;
            }
        }
    }

    private static void Draw(Screen screen, Game game)
    {
        screen.Clear();

        for (int y = 0; y < game.Map.Height; y++)
        {
            for (int x = 0; x < game.Map.Width; x++)
            {
                if (!game.Sight.IsSeen(x, y))
                {
                    continue;
                }

                char tile = game.Map[x, y];
                bool visible = game.Sight.IsVisible(x, y);

                screen.Put(x, y + 1, tile, visible ? TileColor(tile) : DimColor(tile));
            }
        }

        foreach (Item item in game.Map.Items)
        {
            if (!game.Sight.IsVisible(item.X, item.Y))
            {
                continue;
            }

            screen.Put(item.X, item.Y + 1, item.Symbol, ItemColor(item));
        }

        foreach (Monster monster in game.Map.Monsters)
        {
            if (!game.Sight.IsVisible(monster.X, monster.Y))
            {
                continue;
            }

            screen.Put(monster.X, monster.Y + 1, monster.Definition.Symbol, ConsoleColor.Red);
        }

        screen.Put(game.Player.X, game.Player.Y + 1, '@', ConsoleColor.White);

        Player player = game.Player;
        string status =
            $"Level: {game.LevelNumber}  Gold: {player.Gold}  Hp: {player.Hp}({player.MaxHp})  " +
            $"Str: {player.Strength}  Arm: {player.Armor}  Exp: {player.Level}/{player.Experience}  Turn: {game.Turn}";

        screen.Write(0, 0, status, ConsoleColor.Gray);

        string message = game.Message.Length > 0 ? game.Message : Hint;
        screen.Write(0, 23, message, ConsoleColor.Gray);

        screen.Present();
    }

    private static ConsoleKeyInfo ShowInventory(Screen screen, Game game, string prompt)
    {
        Draw(screen, game);

        screen.Write(0, 1, prompt, ConsoleColor.Gray);

        IReadOnlyList<Item> inventory = game.Player.Inventory;

        if (inventory.Count == 0)
        {
            screen.Write(0, 2, "Your pack is empty.", ConsoleColor.Gray);
        }

        for (int i = 0; i < inventory.Count; i++)
        {
            int column = i / 13;
            int row = 2 + i % 13;

            string name = inventory[i].DisplayName(game.Knowledge);
            if (name.Length > 36)
            {
                name = name[..36];
            }

            screen.Write(column * 40, row, $"{(char)('a' + i)}) {name}", ConsoleColor.Gray);
        }

        screen.Write(0, 23, "Letter to choose, Esc to cancel.", ConsoleColor.Gray);
        screen.Present();

        return Console.ReadKey(intercept: true);
    }

    private static int? ChooseItem(Screen screen, Game game, string prompt)
    {
        if (game.Player.Inventory.Count == 0)
        {
            game.Notify("Your pack is empty.");
            return null;
        }

        ConsoleKeyInfo key = ShowInventory(screen, game, prompt);
        if (key.Key == ConsoleKey.Escape)
        {
            return null;
        }

        int index = char.ToLowerInvariant(key.KeyChar) - 'a';
        return index >= 0 && index < game.Player.Inventory.Count ? index : null;
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

    private static ConsoleColor DimColor(char tile) => tile switch
    {
        Tiles.Door or Tiles.StairsDown or Tiles.StairsUp => ConsoleColor.DarkYellow,
        _ => ConsoleColor.DarkGray,
    };

    private static ConsoleColor ItemColor(Item item) => item.Kind switch
    {
        ItemKind.Gold => ConsoleColor.Yellow,
        ItemKind.Potion => ConsoleColor.Magenta,
        ItemKind.Scroll => ConsoleColor.White,
        ItemKind.Food => ConsoleColor.DarkYellow,
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