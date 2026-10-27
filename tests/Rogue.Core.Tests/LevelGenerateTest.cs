using Rogue.Core;

namespace Rogue.Core.Tests;

public class LevelGeneratorTests
{
    [Fact]
    public void Same_seed_produces_the_same_map()
    {
        var first = LevelGenerator.Generate(80, 22, 1234);
        var second = LevelGenerator.Generate(80, 22, 1234);

        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                Assert.Equal(first[x, y], second[x, y]);
            }
        }
    }

    [Fact]
    public void Every_passable_tile_is_reachable_when_secret_doors_count_as_open()
    {
        var level = LevelGenerator.Generate(80, 22, 42);

        int passable = 0;

        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                if (CanPass(level, x, y))
                {
                    passable++;
                }
            }
        }

        Assert.Equal(passable, CountReachable(level, level.UpStairs));
    }

    [Fact]
    public void Level_has_the_classic_tiles_and_both_stairs()
    {
        var level = LevelGenerator.Generate(80, 22, 7);

        Assert.True(level.Rooms.Count >= 4);
        Assert.Equal(Tiles.StairsUp, level[level.UpStairs.X, level.UpStairs.Y]);
        Assert.Equal(Tiles.StairsDown, level[level.DownStairs.X, level.DownStairs.Y]);

        var tiles = new HashSet<char>();

        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                tiles.Add(level[x, y]);
            }
        }

        Assert.Contains(Tiles.Floor, tiles);
        Assert.Contains(Tiles.Corridor, tiles);
        Assert.Contains(Tiles.WallHorizontal, tiles);
        Assert.Contains(Tiles.WallVertical, tiles);
        Assert.True(tiles.Contains(Tiles.Door) || level.SecretDoors.Count > 0);
    }

    [Fact]
    public void Dark_rooms_and_secret_doors_appear_across_seeds()
    {
        bool dark = false;
        bool secret = false;

        for (int seed = 0; seed < 50 && !(dark && secret); seed++)
        {
            var level = LevelGenerator.Generate(80, 22, seed);
            dark |= level.Rooms.Any(room => room.IsDark);
            secret |= level.SecretDoors.Count > 0;
        }

        Assert.True(dark);
        Assert.True(secret);
    }

    private static bool CanPass(Level level, int x, int y)
        => level.IsWalkable(x, y) || level.IsSecretDoor(x, y);

    private static int CountReachable(Level level, (int X, int Y) start)
    {
        var seen = new bool[level.Height, level.Width];
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);
        seen[start.Y, start.X] = true;
        int count = 0;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            count++;

            foreach ((int nx, int ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (!level.InBounds(nx, ny) || seen[ny, nx] || !CanPass(level, nx, ny))
                {
                    continue;
                }

                seen[ny, nx] = true;
                queue.Enqueue((nx, ny));
            }
        }

        return count;
    }
}