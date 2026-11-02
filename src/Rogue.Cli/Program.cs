using Rogue.Core;
using Rogue.Cli.Input;

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
        var store = new SaveStore();

        Game game;

        if (store.TryLoad(out SaveData? save))
        {
            try
            {
                game = Game.Restore(save!);
                store.DeleteSave();
                game.Notify("Welcome back.");
            }
            catch (Exception)
            {
                store.DeleteSave();
                game = new Game();
            }
        }
        else
        {
            game = new Game();
        }

        while (true)
        {
            Draw(screen, game);

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Escape
                || (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key is ConsoleKey.C or ConsoleKey.Q))
            {
                if (!game.IsDead && !game.IsWon)
                {
                    try
                    {
                        store.Save(game.Snapshot());
                    }
                    catch (Exception)
                    {
                        // 存档失败不应该阻止退出
                    }
                }

                return;
            }

                        KeyBinding? resolved = InputResolver.Resolve(key);
            if (resolved is not { } binding)
            {
                continue;
            }

            switch (binding.Action)
            {
                case GameAction.Move:
                    game.TryMove(binding.Dx, binding.Dy);
                    break;

                case GameAction.Wait:
                    game.Wait();
                    break;

                case GameAction.PickUp:
                    game.PickUp();
                    break;

                case GameAction.Inventory:
                    ShowInventory(screen, game, "Inventory");
                    break;

                case GameAction.Quaff:
                    if (ChooseItem(screen, game, "Quaff which item?") is int quaff)
                    {
                        game.Quaff(quaff);
                    }
                    break;

                case GameAction.Read:
                    if (ChooseItem(screen, game, "Read which item?") is int read)
                    {
                        game.Read(read);
                    }
                    break;

                case GameAction.Eat:
                    if (ChooseItem(screen, game, "Eat which item?") is int eat)
                    {
                        game.Eat(eat);
                    }
                    break;

                case GameAction.Wield:
                    if (ChooseItem(screen, game, "Wield which item?") is int wield)
                    {
                        game.Wield(wield);
                    }
                    break;

                case GameAction.ToggleArmor:
                    game.ToggleArmor();
                    break;
    
                case GameAction.Drop:
                    if (ChooseItem(screen, game, "Drop which item?") is int drop)
                    {
                        game.Drop(drop);
                    }
                    break;

                case GameAction.Search:
                    game.Search();
                    break;

                case GameAction.UseStairs:
                    game.UseStairs();
                    break;
            }

            if (game.IsWon)
            {
                Finish(store, game, won: true);
                return;
            }

            if (game.IsDead)
            {
                Finish(store, game, won: false);
                return;
            }
        }
    }

    private static void Finish(SaveStore store, Game game, bool won)
    {
        try
        {
            store.DeleteSave();
        }
        catch (Exception)
        {
        }

        var entry = new ScoreEntry
        {
            Name = Environment.UserName,
            Score = game.Score,
            Level = game.LevelNumber,
            Gold = game.Player.Gold,
            Experience = game.Player.Experience,
            Won = won,
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
        };

        List<ScoreEntry> scores;

        try
        {
            scores = store.AddScore(entry);
        }
        catch (Exception)
        {
            scores = new List<ScoreEntry> { entry };
        }

        RestoreTerminal();

        Console.WriteLine();
        Console.WriteLine(won
            ? "You escaped the Dungeon of Doom with the Amulet of Yendor!"
            : game.Message);
        Console.WriteLine();
        Console.WriteLine($"Score: {entry.Score}   Gold: {entry.Gold}   Exp: {entry.Experience}   Depth: {entry.Level}");
        Console.WriteLine();
        Console.WriteLine("High scores");
        Console.WriteLine("-----------");

        for (int i = 0; i < scores.Count; i++)
        {
            ScoreEntry score = scores[i];
            string mark = score.Won ? "escaped" : $"level {score.Level}";
            Console.WriteLine($"{i + 1,2}. {score.Name,-12} {score.Score,6}  {mark}");
        }

        Console.WriteLine();
        Console.Write("Press any key to exit.");
        Console.ReadKey(intercept: true);
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

        foreach (Trap trap in game.Map.Traps)
        {
            if (!trap.IsFound || !game.Sight.IsVisible(trap.X, trap.Y))
            {
                continue;
            }

            screen.Put(trap.X, trap.Y + 1, '^', ConsoleColor.DarkRed);
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

        string message = game.Message.Length > 0 ? game.Message : KeyMap.Hint;
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
        ItemKind.Amulet => ConsoleColor.Yellow,
        _ => ConsoleColor.Gray,
    };


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