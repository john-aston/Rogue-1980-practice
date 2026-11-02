namespace Rogue.Cli.Input;

public static class InputResolver
{
    public static KeyBinding? Resolve(ConsoleKeyInfo key)
    {
        // 1. 方向键这类没有字符的键
        if (KeyMap.TryGetKey(key.Key, out KeyBinding special))
        {
            return special;
        }

        // 2. 其余大小写不敏感
        if (KeyMap.TryGetLower(char.ToLowerInvariant(key.KeyChar), out KeyBinding lower))
        {
            return lower;
        }

        return null;
    }
}