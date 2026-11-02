namespace Rogue.Cli.Input;

public static class KeyMap
{
    public const string Hint =
        "方向键移动 | g 获取 | i 查看背包 | q r e use | w W T equip | d drop | s 搜索 | < > 上下楼 | . wait";



    // 其余统一按大写转小写查
    private static readonly Dictionary<char, KeyBinding> Lower = new()
    {
        ['g'] = new(GameAction.PickUp),
        ['i'] = new(GameAction.Inventory),
        ['q'] = new(GameAction.Quaff),
        ['r'] = new(GameAction.Read),
        ['e'] = new(GameAction.Eat),
        ['w'] = new(GameAction.Wield),
        ['d'] = new(GameAction.Drop),
        ['s'] = new(GameAction.Search),
        ['.'] = new(GameAction.Wait),
    };

    private static readonly Dictionary<ConsoleKey, KeyBinding> Keys = new()
    {
        [ConsoleKey.LeftArrow] = new(GameAction.Move, -1, 0),
        [ConsoleKey.RightArrow] = new(GameAction.Move, 1, 0),
        [ConsoleKey.UpArrow] = new(GameAction.Move, 0, -1),
        [ConsoleKey.DownArrow] = new(GameAction.Move, 0, 1),
        [ConsoleKey.Enter] = new(GameAction.UseStairs),
    };

    public static bool TryGetKey(ConsoleKey key, out KeyBinding binding)
        => Keys.TryGetValue(key, out binding);

    public static bool TryGetLower(char keyChar, out KeyBinding binding)
        => Lower.TryGetValue(keyChar, out binding);
}