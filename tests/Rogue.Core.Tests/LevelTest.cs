using Rogue.Core;

namespace Rogue.Core.Tests;

public class LevelTests
{
    [Fact]
    public void Revealing_a_secret_door_turns_it_into_a_door()
    {
        var level = Level.FromAscii(
            "#####",
            "#|..#",
            "#...#",
            "#####");
        level.AddSecretDoor(1, 1, Tiles.WallVertical);

        Assert.True(level.IsSecretDoor(1, 1));
        Assert.False(level.IsWalkable(1, 1));

        Assert.True(level.RevealSecretDoor(1, 1));
        Assert.False(level.IsSecretDoor(1, 1));
        Assert.True(level.IsWalkable(1, 1));
        Assert.Equal(Tiles.Door, level[1, 1]);
    }
}