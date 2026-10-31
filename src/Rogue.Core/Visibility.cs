namespace Rogue.Core;

//视野与记忆：Visible 是现在能看到的，Seen 是曾经看到过的
public sealed class Visibility
{
    private readonly Level _level;
    private readonly bool[,] _visible;
    private readonly bool[,] _seen;

    public Visibility(Level level)
    {
        _level = level;
        _visible = new bool[level.Height, level.Width];
        _seen = new bool[level.Height, level.Width];
    }

    public bool IsVisible(int x, int y)
        => _level.InBounds(x, y) && _visible[y, x];

    public bool IsSeen(int x, int y)
        => _level.InBounds(x, y) && _seen[y, x];

    /// <summary>重新计算玩家周围能看到的范围。</summary>
    public void Update(int playerX, int playerY)
    {
        Array.Clear(_visible);

        Room? room = _level.RoomAt(playerX, playerY);

        // 点亮的房间：整间房（含墙和门）都能看见
        if (room is { IsDark: false })
        {
            for (int y = room.Y; y <= room.Bottom; y++)
            {
                for (int x = room.X; x <= room.Right; x++)
                {
                    Show(x, y);
                }
            }

            return;
        }

        // 暗房间、走廊、门口：只能看清身边一圈（原版 look() 的 3×3）
        for (int y = playerY - 1; y <= playerY + 1; y++)
        {
            for (int x = playerX - 1; x <= playerX + 1; x++)
            {
                Show(x, y);
            }
        }
    }

    private void Show(int x, int y)
    {
        if (!_level.InBounds(x, y))
        {
            return;
        }

        _visible[y, x] = true;
        _seen[y, x] = true;
    }

    /// <summary>魔法地图：把整层的地形都标成“看过”。</summary>
    public void RevealAll()
    {
        for (int y = 0; y < _level.Height; y++)
        {
            for (int x = 0; x < _level.Width; x++)
            {
                if (_level[x, y] != Tiles.Rock)
                {
                    _seen[y, x] = true;
                }
            }
        }
    }
}