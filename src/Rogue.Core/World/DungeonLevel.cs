namespace Rogue.Core;

/// <summary>一层地牢，连同它自己的“看过哪些格子”的记忆。</summary>
public sealed class DungeonLevel
{
    public DungeonLevel(Level map)
    {
        Map = map;
        Sight = new Visibility(map);
    }

    public Level Map { get; }
    public Visibility Sight { get; }
}