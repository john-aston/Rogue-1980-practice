using Rogue.Core;

namespace Rogue.Core.Tests;

public class GameTests
{
    [Fact]
    public void Searching_eventually_finds_an_adjacent_secret_door()
    {
        var level = Level.FromAscii(
            "#####",
            "#|..#",
            "#...#",
            "#####");
        level.AddSecretDoor(1, 1, Tiles.WallVertical);
        var game = new Game(level, 2, 1);

        bool found = false;

        for (int i = 0; i < 200 && !found; i++)
        {
            found = game.Search();
        }

        Assert.True(found);
        Assert.Equal(Tiles.Door, level[1, 1]);
    }
    
}