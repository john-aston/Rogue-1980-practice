//self-made 移动规则测试
using Rogue.Core;

namespace Rogue.Core.Tests;

public class GameTests
{
    [Fact]
    public void Walking_into_a_wall_does_not_move_or_advance_the_turn()
    {
        var game = new Game();
        int startX = game.Player.X;
        int startY = game.Player.Y;

        Assert.True(game.TryMove(0, -1));
        Assert.True(game.TryMove(0, -1));
        Assert.False(game.TryMove(0, -1));   // 再往上就是墙

        Assert.Equal(startX, game.Player.X);
        Assert.Equal(startY - 2, game.Player.Y);
        Assert.Equal(2, game.Turn);
    }
}