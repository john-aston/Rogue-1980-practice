namespace Rogue.Core;

/// <summary>
/// 按原版思路生成一层：屏幕分成 3×3 扇区，每个扇区最多一个房间；
/// 只有上下左右相邻的房间之间才挖走廊；最后放上下楼梯。
/// </summary>
public static class LevelGenerator
{
    private const int GridSize = 3;
    private const int MinRoomSize = 4;
    private const int DarkRoomChance = 10;
    private const int SecretDoorChance = 20;

    public static Level Generate(int width, int height, int seed)
    {
        var rng = new Random(seed);
        var level = new Level(width, height);

        Room?[,] grid = PlaceRooms(level, width, height, rng);
        ConnectRooms(level, grid, rng);
        PlaceStairs(level, grid, rng);

        return level;
    }

    private static Room?[,] PlaceRooms(Level level, int width, int height, Random rng)
    {
        bool[,] present = ChooseSectors(rng);
        var grid = new Room?[GridSize, GridSize];

        for (int gy = 0; gy < GridSize; gy++)
        {
            for (int gx = 0; gx < GridSize; gx++)
            {
                if (!present[gy, gx])
                {
                    continue;
                }

                int left = gx * width / GridSize;
                int right = (gx + 1) * width / GridSize;
                int top = gy * height / GridSize;
                int bottom = (gy + 1) * height / GridSize;

                // 扇区里留 1 格边距，保证相邻扇区的房间不会贴在一起
                int maxWidth = right - left - 2;
                int maxHeight = bottom - top - 2;

                int roomWidth = rng.Next(MinRoomSize, maxWidth + 1);
                int roomHeight = rng.Next(MinRoomSize, maxHeight + 1);
                int roomX = left + 1 + rng.Next(0, right - left - 1 - roomWidth);
                int roomY = top + 1 + rng.Next(0, bottom - top - 1 - roomHeight);

                bool isDark = rng.Next(100) < DarkRoomChance;
                var room = new Room(roomX, roomY, roomWidth, roomHeight, isDark);

                level.DrawRoom(room.X, room.Y, room.Width, room.Height);
                level.AddRoom(room);
                grid[gy, gx] = room;
            }
        }

        return grid;
    }

    /// <summary>决定九宫格里哪些位置有房间，并保证它们能连成一片。</summary>
    private static bool[,] ChooseSectors(Random rng)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var present = new bool[GridSize, GridSize];
            int count = 0;

            for (int gy = 0; gy < GridSize; gy++)
            {
                for (int gx = 0; gx < GridSize; gx++)
                {
                    if (rng.Next(100) < 85)
                    {
                        present[gy, gx] = true;
                        count++;
                    }
                }
            }

            while (count < 4)
            {
                int gx = rng.Next(GridSize);
                int gy = rng.Next(GridSize);

                if (present[gy, gx])
                {
                    continue;
                }

                present[gy, gx] = true;
                count++;
            }

            if (IsConnected(present))
            {
                return present;
            }
        }

        var full = new bool[GridSize, GridSize];

        for (int gy = 0; gy < GridSize; gy++)
        {
            for (int gx = 0; gx < GridSize; gx++)
            {
                full[gy, gx] = true;
            }
        }

        return full;
    }

    private static bool IsConnected(bool[,] present)
    {
        var seen = new bool[GridSize, GridSize];
        int total = 0;
        int startX = -1;
        int startY = -1;

        for (int gy = 0; gy < GridSize; gy++)
        {
            for (int gx = 0; gx < GridSize; gx++)
            {
                if (!present[gy, gx])
                {
                    continue;
                }

                total++;

                if (startX < 0)
                {
                    startX = gx;
                    startY = gy;
                }
            }
        }

        if (startX < 0)
        {
            return false;
        }

        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((startX, startY));
        seen[startY, startX] = true;
        int visited = 0;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            visited++;

            foreach ((int nx, int ny) in Neighbors(x, y))
            {
                if (!present[ny, nx] || seen[ny, nx])
                {
                    continue;
                }

                seen[ny, nx] = true;
                queue.Enqueue((nx, ny));
            }
        }

        return visited == total;
    }

    private static void ConnectRooms(Level level, Room?[,] grid, Random rng)
    {
        var present = new List<(int X, int Y)>();

        for (int gy = 0; gy < GridSize; gy++)
        {
            for (int gx = 0; gx < GridSize; gx++)
            {
                if (grid[gy, gx] is not null)
                {
                    present.Add((gx, gy));
                }
            }
        }

        if (present.Count == 0)
        {
            return;
        }

        var connected = new HashSet<(int X, int Y)>
        {
            present[rng.Next(present.Count)],
        };
        var linked = new HashSet<(int, int, int, int)>();

        // 生成一棵连通所有房间的树：每次挑一条“已连通 → 未连通”的边
        while (connected.Count < present.Count)
        {
            var edges = new List<((int X, int Y) From, (int X, int Y) To)>();

            foreach ((int x, int y) in connected)
            {
                foreach ((int nx, int ny) in Neighbors(x, y))
                {
                    if (grid[ny, nx] is not null && !connected.Contains((nx, ny)))
                    {
                        edges.Add(((x, y), (nx, ny)));
                    }
                }
            }

            if (edges.Count == 0)
            {
                break;
            }

            ((int X, int Y) from, (int X, int Y) to) = edges[rng.Next(edges.Count)];
            ConnectPair(level, grid, from, to, rng);
            linked.Add(PairKey(from, to));
            connected.Add(to);
        }

        // 原版还会随机加几条额外通道，让地图不是一棵纯树
        for (int gy = 0; gy < GridSize; gy++)
        {
            for (int gx = 0; gx < GridSize; gx++)
            {
                if (grid[gy, gx] is null)
                {
                    continue;
                }

                foreach ((int nx, int ny) in Neighbors(gx, gy))
                {
                    if (nx < gx || ny < gy)
                    {
                        continue;
                    }

                    if (grid[ny, nx] is null || linked.Contains(PairKey((gx, gy), (nx, ny))))
                    {
                        continue;
                    }

                    if (rng.Next(100) < 20)
                    {
                        ConnectPair(level, grid, (gx, gy), (nx, ny), rng);
                    }
                }
            }
        }
    }

    private static void ConnectPair(
        Level level,
        Room?[,] grid,
        (int X, int Y) a,
        (int X, int Y) b,
        Random rng)
    {
        Room roomA = grid[a.Y, a.X]!;
        Room roomB = grid[b.Y, b.X]!;

        if (a.Y == b.Y)
        {
            ConnectHorizontally(level, roomA, roomB, rng);
        }
        else
        {
            ConnectVertically(level, roomA, roomB, rng);
        }
    }

    /// <summary>左右相邻：从两边墙各开一扇门，走廊在两扇门之间的空档里拐弯。</summary>
    private static void ConnectHorizontally(Level level, Room left, Room right, Random rng)
    {
        if (left.X > right.X)
        {
            (left, right) = (right, left);
        }

        int y1 = rng.Next(left.Y + 1, left.Bottom);
        int y2 = rng.Next(right.Y + 1, right.Bottom);
        int x1 = left.Right;
        int x2 = right.X;
        int midX = (x1 + x2) / 2;

        PlaceDoor(level, x1, y1, Tiles.WallVertical, rng);
        PlaceDoor(level, x2, y2, Tiles.WallVertical, rng);

        for (int x = x1 + 1; x <= midX; x++)
        {
            level.CarveCorridor(x, y1);
        }

        for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
        {
            level.CarveCorridor(midX, y);
        }

        for (int x = midX; x <= x2 - 1; x++)
        {
            level.CarveCorridor(x, y2);
        }
    }

    /// <summary>上下相邻：同样的思路，只是换成从上下墙开门。</summary>
    private static void ConnectVertically(Level level, Room top, Room bottom, Random rng)
    {
        if (top.Y > bottom.Y)
        {
            (top, bottom) = (bottom, top);
        }

        int x1 = rng.Next(top.X + 1, top.Right);
        int x2 = rng.Next(bottom.X + 1, bottom.Right);
        int y1 = top.Bottom;
        int y2 = bottom.Y;
        int midY = (y1 + y2) / 2;

        PlaceDoor(level, x1, y1, Tiles.WallHorizontal, rng);
        PlaceDoor(level, x2, y2, Tiles.WallHorizontal, rng);

        for (int y = y1 + 1; y <= midY; y++)
        {
            level.CarveCorridor(x1, y);
        }

        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        {
            level.CarveCorridor(x, midY);
        }

        for (int y = midY; y <= y2 - 1; y++)
        {
            level.CarveCorridor(x2, y);
        }
    }

    /// <summary>20% 的机会把门伪装成墙（秘密门）。</summary>
    private static void PlaceDoor(Level level, int x, int y, char disguise, Random rng)
    {
        if (rng.Next(100) < SecretDoorChance)
        {
            level.AddSecretDoor(x, y, disguise);
        }
        else
        {
            level.Set(x, y, Tiles.Door);
        }
    }

    private static void PlaceStairs(Level level, Room?[,] grid, Random rng)
    {
        var rooms = new List<Room>();

        for (int gy = 0; gy < GridSize; gy++)
        {
            for (int gx = 0; gx < GridSize; gx++)
            {
                if (grid[gy, gx] is { } room)
                {
                    rooms.Add(room);
                }
            }
        }

        if (rooms.Count == 0)
        {
            return;
        }

        Room start = rooms[rng.Next(rooms.Count)];
        (int X, int Y) up = RandomFloor(start, rng);
        level.Set(up.X, up.Y, Tiles.StairsUp);
        level.UpStairs = up;

        if (rooms.Count == 1)
        {
            return;
        }

        // 下楼放得离起点远一点，让每层都值得走一遍
        Room far = start;
        int best = -1;

        foreach (Room room in rooms)
        {
            if (room == start)
            {
                continue;
            }

            int distance = Math.Abs(room.CenterX - start.CenterX)
                         + Math.Abs(room.CenterY - start.CenterY);

            if (distance > best)
            {
                best = distance;
                far = room;
            }
        }

        (int X, int Y) down = RandomFloor(far, rng);
        level.Set(down.X, down.Y, Tiles.StairsDown);
        level.DownStairs = down;
    }

    private static (int X, int Y) RandomFloor(Room room, Random rng)
        => (rng.Next(room.X + 1, room.Right), rng.Next(room.Y + 1, room.Bottom));

    private static IEnumerable<(int X, int Y)> Neighbors(int x, int y)
    {
        if (x > 0)
        {
            yield return (x - 1, y);
        }

        if (x < GridSize - 1)
        {
            yield return (x + 1, y);
        }

        if (y > 0)
        {
            yield return (x, y - 1);
        }

        if (y < GridSize - 1)
        {
            yield return (x, y + 1);
        }
    }

    private static (int, int, int, int) PairKey((int X, int Y) a, (int X, int Y) b)
        => a.X < b.X || (a.X == b.X && a.Y < b.Y)
            ? (a.X, a.Y, b.X, b.Y)
            : (b.X, b.Y, a.X, a.Y);
}