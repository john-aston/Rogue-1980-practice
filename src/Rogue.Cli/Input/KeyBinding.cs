namespace Rogue.Cli.Input;

public readonly record struct KeyBinding(GameAction Action, int Dx = 0, int Dy = 0);