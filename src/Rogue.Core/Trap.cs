namespace Rogue.Core;

public enum TrapType
{
    TrapDoor,
    Arrow,
    PoisonNeedle,
    Teleport,
}

public sealed class Trap
{
    public Trap(TrapType type, int x, int y)
    {
        Type = type;
        X = x;
        Y = y;
    }

    public TrapType Type { get; }
    public int X { get; }
    public int Y { get; }
    public bool IsFound { get; set; }
}
