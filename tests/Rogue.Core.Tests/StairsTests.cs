using Rogue.Core;

namespace Rogue.Core.Tests;

public class StairsTests
{
    private static void StandOnDownStairs(Game game)
    {
        game.Player.X = game.Map.DownStairs.X;
        game.Player.Y = game.Map.DownStairs.Y;
    }

    [Fact]
    public void Descending_moves_the_player_to_the_up_stairs_below()
    {
        var game = new Game(1234);
        StandOnDownStairs(game);

        Assert.True(game.TryDescend());
        Assert.Equal(2, game.LevelNumber);
        Assert.Equal(Tiles.StairsUp, game.Map[game.Player.X, game.Player.Y]);
    }

    [Fact]
    public void Ascending_moves_the_player_to_the_down_stairs_above()
    {
        var game = new Game(1234);
        StandOnDownStairs(game);
        Assert.True(game.TryDescend());

        Assert.True(game.TryAscend());
        Assert.Equal(1, game.LevelNumber);
        Assert.Equal(Tiles.StairsDown, game.Map[game.Player.X, game.Player.Y]);
    }

    [Fact]
    public void Levels_keep_their_state_between_visits()
    {
        var game = new Game(42);
        StandOnDownStairs(game);
        Assert.True(game.TryDescend());

        Level levelTwo = game.Map;
        char tile = levelTwo[10, 10];

        Assert.True(game.TryAscend());
        StandOnDownStairs(game);
        Assert.True(game.TryDescend());

        Assert.Same(levelTwo, game.Map);
        Assert.Equal(tile, game.Map[10, 10]);
    }

    [Fact]
    public void Level_one_has_no_way_up()
    {
        var game = new Game(1234);

        Assert.Equal(Tiles.StairsUp, game.Map[game.Player.X, game.Player.Y]);
        Assert.False(game.TryAscend());
        Assert.Equal(1, game.LevelNumber);
    }

    [Fact]
    public void Descending_requires_standing_on_the_down_stairs()
    {
        var game = new Game(1234);

        Assert.False(game.TryDescend());
        Assert.Equal(1, game.LevelNumber);
    }

    [Fact]
    public void Stairs_do_not_pass_a_turn()
    {
        var game = new Game(1234);
        StandOnDownStairs(game);

        Assert.True(game.TryDescend());
        Assert.Equal(0, game.Turn);
    }

    [Fact]
    public void Deepest_level_has_no_down_stairs()
    {
        var game = new Game(7);

        for (int level = 1; level < Game.MaxDepth; level++)
        {
            StandOnDownStairs(game);
            Assert.True(game.TryDescend());
        }

        Assert.Equal(Game.MaxDepth, game.LevelNumber);
        Assert.Equal(-1, game.Map.DownStairs.X);
        Assert.False(game.TryDescend());
    }
}