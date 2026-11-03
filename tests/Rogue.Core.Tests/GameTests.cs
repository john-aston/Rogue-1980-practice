using Rogue.Core;

namespace Rogue.Core.Tests;

public class GameTests
{
    [Fact]
    public void Player_starts_on_the_up_stairs()
    {
        var game = new Game(1234);

        Assert.Equal(Tiles.StairsUp, game.Map[game.Player.X, game.Player.Y]);
    }

    [Fact]
    public void Walking_into_a_wall_does_not_move_or_advance_the_turn()
    {
        var level = Level.FromAscii(
            "-----",
            "|...|",
            "-----");

        var game = new Game(level, 2, 1);

        Assert.False(game.TryMove(0, -1));
        Assert.Equal(2, game.Player.X);
        Assert.Equal(1, game.Player.Y);
        Assert.Equal(0, game.Turn);

        Assert.True(game.TryMove(-1, 0));
        Assert.Equal(1, game.Player.X);
        Assert.Equal(1, game.Turn);
    }
}
