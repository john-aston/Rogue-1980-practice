using Rogue.Core;

namespace Rogue.Core.Tests;

public class LevelTests
{
    [Fact]
    public void FromAscii_maps_characters_to_walkability()
    {
        var level = Level.FromAscii(
            "-----",
            "|...|",
            "|<+>|",
            "-----");

        Assert.True(level.IsWalkable(1, 1));
        Assert.True(level.IsWalkable(1, 2));
        Assert.True(level.IsWalkable(2, 2));
        Assert.False(level.IsWalkable(0, 0));
        Assert.False(level.IsWalkable(-1, 1));
        Assert.False(level.IsWalkable(5, 1));
    }
}
