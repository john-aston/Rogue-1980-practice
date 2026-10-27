using Rogue.Core;

namespace Rogue.Core.Tests;

public class VisibilityTests
{
    private static Level BuildLevel(bool isDark)
    {
        var level = Level.FromAscii(
            "#####....",
            "#...#....",
            "#...#....",
            "#...#....",
            "#####....",
            ".........");

        level.AddRoom(new Room(0, 0, 5, 5, isDark));
        return level;
    }

    [Fact]
    public void Lit_room_reveals_the_whole_room()
    {
        Level level = BuildLevel(isDark: false);
        var sight = new Visibility(level);

        sight.Update(2, 2);

        Assert.True(sight.IsVisible(1, 1));
        Assert.True(sight.IsVisible(4, 4));
        Assert.False(sight.IsVisible(6, 2));
    }

    [Fact]
    public void Dark_room_only_reveals_the_three_by_three_around_the_player()
    {
        Level level = BuildLevel(isDark: true);
        var sight = new Visibility(level);

        sight.Update(2, 2);

        Assert.True(sight.IsVisible(1, 1));
        Assert.True(sight.IsVisible(3, 3));
        Assert.False(sight.IsVisible(4, 2));
        Assert.False(sight.IsVisible(1, 4));
    }

    [Fact]
    public void Seen_tiles_are_remembered_after_moving_away()
    {
        Level level = BuildLevel(isDark: false);
        var sight = new Visibility(level);

        sight.Update(2, 2);
        sight.Update(7, 2);

        Assert.True(sight.IsSeen(2, 2));
        Assert.False(sight.IsVisible(2, 2));
        Assert.True(sight.IsVisible(7, 2));
    }
}