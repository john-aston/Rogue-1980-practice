namespace Rogue.Cli.Input;

public static class KeyMap
{
    public const string Hint =
        "Arrows move  Enter stairs  q potion  r scroll  e food  u upgrades  . wait";

    private static readonly Dictionary<char, KeyBinding> Lower = new()
    {
        ['q'] = new(GameAction.Quaff),
        ['r'] = new(GameAction.Read),
        ['e'] = new(GameAction.Eat),
        ['u'] = new(GameAction.Upgrade),
        ['.'] = new(GameAction.Wait),
    };

    private static readonly Dictionary<ConsoleKey, KeyBinding> Keys = new()
    {
        [ConsoleKey.LeftArrow] = new(GameAction.Move, -1, 0),
        [ConsoleKey.RightArrow] = new(GameAction.Move, 1, 0),
        [ConsoleKey.UpArrow] = new(GameAction.Move, 0, -1),
        [ConsoleKey.DownArrow] = new(GameAction.Move, 0, 1),
        [ConsoleKey.Enter] = new(GameAction.Interact),
    };

    public static bool TryGetKey(ConsoleKey key, out KeyBinding binding)
        => Keys.TryGetValue(key, out binding);

    public static bool TryGetLower(char keyChar, out KeyBinding binding)
        => Lower.TryGetValue(keyChar, out binding);
}
