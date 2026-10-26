//self-made 地图规则测试
using Rogue.Core;

namespace Rogue.Core.Tests;

public class LevelTests
{
    [Fact]
    public void Room_floor_is_walkable()
    {
        Level level = Level.CreateDemo();
        Assert.True(level.IsWalkable(4, 4));
    }

    [Fact]
    public void Solid_rock_is_not_walkable()
    {
        Level level = Level.CreateDemo();
        Assert.False(level.IsWalkable(0, 0));
    }

    [Fact]
    public void Outside_the_map_is_not_walkable()
    {
        Level level = Level.CreateDemo();
        Assert.False(level.IsWalkable(-1, 5));
        Assert.False(level.IsWalkable(level.Width, 5));
    }
}