namespace Rogue.Cli.Input;

public static class InputResolver
{
    public static KeyBinding? Resolve(ConsoleKeyInfo key)
    {
        if (KeyMap.TryGetKey(key.Key, out KeyBinding special))
        {
            return special;
        }

        if (KeyMap.TryGetLower(char.ToLowerInvariant(key.KeyChar), out KeyBinding lower))
        {
            return lower;
        }

        return null;
    }
}