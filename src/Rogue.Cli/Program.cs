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
        bool resumed = false;

        if (store.TryLoad(out SaveData? save))
        {
            try
            {
                game = Game.Restore(save!);
                store.DeleteSave();
                resumed = true;
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

        if (!resumed)
        {
            ShowTitle(screen);
        }
        else
        {
            game.Notify("Welcome back.");
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
                    }
                }

                return;
            }

            if (key.KeyChar == '?')
            {
                ShowHelp(screen, game);
                continue;
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

                case GameAction.Interact:
                    game.UseStairs();
                    break;

                case GameAction.Quaff:
                    game.Quaff();
                    break;

                case GameAction.Read:
                    game.Read();
                    break;

                case GameAction.Eat:
                    game.Eat();
                    break;

                case GameAction.Upgrade:
                    ShowUpgradeMenu(screen, game);
                    break;

                case GameAction.Wait:
                    game.Wait();
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

    private static void ShowTitle(Screen screen)
    {
        screen.Clear();
        screen.Write(24, 8, "R  O  G  U  E", ConsoleColor.White);
        screen.Write(24, 10, "A tiny ASCII dungeon crawl", ConsoleColor.Gray);
        screen.Write(20, 14, "Arrows move. Enter uses stairs.", ConsoleColor.Gray);
        screen.Write(20, 15, "q potion   r scroll   e food   u upgrades", ConsoleColor.Gray);
        screen.Write(24, 18, "Press any key to start.", ConsoleColor.Yellow);
        screen.Present();
        Console.ReadKey(intercept: true);
    }

    private static void ShowHelp(Screen screen, Game game)
    {
        Draw(screen, game);

        screen.Write(2, 2, "Controls", ConsoleColor.White);
        screen.Write(2, 4, "Arrows        move", ConsoleColor.Gray);
        screen.Write(2, 5, "Enter         use stairs", ConsoleColor.Gray);
        screen.Write(2, 6, "q             drink a healing potion", ConsoleColor.Gray);
        screen.Write(2, 7, "r             read a teleport scroll", ConsoleColor.Gray);
        screen.Write(2, 8, "e             eat a food ration", ConsoleColor.Gray);
        screen.Write(2, 9, "u             spend gold on upgrades", ConsoleColor.Gray);
        screen.Write(2, 10, ".             wait one turn", ConsoleColor.Gray);
        screen.Write(2, 11, "Esc           save and quit", ConsoleColor.Gray);
        screen.Write(2, 13, "! potion   ? scroll   % food   * gold   , Amulet", ConsoleColor.Gray);
        screen.Write(2, 15, "Press any key to close.", ConsoleColor.Yellow);
        screen.Present();
        Console.ReadKey(intercept: true);
    }

    private static void ShowUpgradeMenu(Screen screen, Game game)
    {
        while (true)
        {
            Draw(screen, game);

            Player player = game.Player;

            screen.Write(2, 2, $"Gold: {player.Gold}", ConsoleColor.Yellow);

            screen.Write(2, 4, "1) Sharpen sword   " + SwordCost(player), ConsoleColor.Gray);
            screen.Write(2, 5, "2) Reinforce armor " + ArmorCost(player), ConsoleColor.Gray);
            screen.Write(2, 6, $"3) Buy potion      25 gold   ({player.Potions}/{Player.PotionLimit})", ConsoleColor.Gray);
            screen.Write(2, 7, $"4) Buy food        15 gold   ({player.Rations})", ConsoleColor.Gray);
            screen.Write(2, 8, "5) +2 max HP       60 gold", ConsoleColor.Gray);
            screen.Write(2, 10, "1-5 to buy, Esc to close.", ConsoleColor.Yellow);
            screen.Present();

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Escape)
            {
                return;
            }

            if (key.KeyChar is >= '1' and <= '5')
            {
                game.Buy(key.KeyChar - '0');
            }
        }
    }

    private static string SwordCost(Player player)
        => player.SwordLevel >= Game.MaxUpgradeLevel
            ? $"max (+{player.SwordLevel})"
            : $"{Game.UpgradeCostBase * (player.SwordLevel + 1)} gold   (+{player.SwordLevel})";

    private static string ArmorCost(Player player)
        => player.ArmorLevel >= Game.MaxUpgradeLevel
            ? $"max (+{player.ArmorLevel})"
            : $"{Game.UpgradeCostBase * (player.ArmorLevel + 1)} gold   (+{player.ArmorLevel})";

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
            $"D{game.LevelNumber} HP[{HpBar(player)}]{player.Hp}/{player.MaxHp} " +
            $"G{player.Gold} Food{player.Food} P{player.Potions}/{Player.PotionLimit} " +
            $"S{player.Scrolls} R{player.Rations} Sw+{player.SwordLevel} AC{player.Armor}";

        screen.Write(0, 0, status, player.Hp * 3 <= player.MaxHp ? ConsoleColor.Red : ConsoleColor.Gray);

        string message = game.Message.Length > 0 ? game.Message : KeyMap.Hint;
        ConsoleColor messageColor = game.Tone switch
        {
            MessageTone.Good => ConsoleColor.Green,
            MessageTone.Bad => ConsoleColor.Red,
            _ => ConsoleColor.Gray,
        };

        screen.Write(0, 23, message, messageColor);
        screen.Present();
    }

    private static string HpBar(Player player)
    {
        int filled = player.MaxHp <= 0
            ? 0
            : Math.Clamp(player.Hp * 10 / player.MaxHp, 0, 10);

        return new string('#', filled) + new string('-', 10 - filled);
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
        Console.WriteLine($"Score: {entry.Score}   Gold: {entry.Gold}   Depth: {entry.Level}");
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
