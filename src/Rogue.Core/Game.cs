//self-made 一局游戏的状态，TryMove 规则

using Rogue.Core;

namespace Rogue.Core;

/// <summary>全部状态。以后所有改变世界的操作都从这里走。</summary>
public sealed class Game
{
    public Level Map{ get; }
    public Player Player{ get; }
    public int Turn { get; private set; }

    public Game()
    {
        Map = Level.CreateDemo();
        Player = new Player { X = 4, Y = 4};
    }

    public bool TryMove(int dx, int dy)
    {
        int nextX = Player.X + dx;
        int nextY = Player.Y + dy;

        if (!Map.IsWalkable(nextX, nextY))
        {
            return false;
        }

        Player.X = nextX;
        Player.Y = nextY;
        Turn++;
        return true;    
    }
}
