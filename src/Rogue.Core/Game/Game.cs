namespace Rogue.Core;

public enum MessageTone
{
    Normal,
    Good,
    Bad,
}

public sealed partial class Game
{
    public const int MapWidth = 80;
    public const int MapHeight = 22;
    public const int MaxDepth = 26;
    public const int WakeDistance = 8;
    public const int MaxUpgradeLevel = 5;
    public const int UpgradeCostBase = 40;

    private readonly Random _rng;
    private readonly int _seed;
    private readonly Dictionary<int, DungeonLevel> _levels = new();
    private readonly List<(string Text, MessageTone Tone)> _messages = new();
    private bool _hungryShown;
    private bool _weakShown;
    private string? _deathCause;
    private DungeonLevel _current;

    public Player Player { get; }
    public int Turn { get; private set; }
    public int LevelNumber { get; private set; }
    public bool IsDead => Player.Hp <= 0;
    public bool IsWon { get; private set; }

    public int Score => Player.Gold + LevelNumber * 10 + (Player.HasAmulet ? 1000 : 0);

    public Level Map => _current.Map;
    public Visibility Sight => _current.Sight;
    public string Message => _messages.Count == 0 ? "" : _messages[^1].Text;
    public MessageTone Tone => _messages.Count == 0 ? MessageTone.Normal : _messages[^1].Tone;

    public Game() : this(Random.Shared.Next())
    {
    }

    public Game(int seed)
    {
        _seed = seed;
        _rng = new Random(seed);
        Player = new Player();

        LevelNumber = 1;
        _current = LoadLevel(LevelNumber);
        MovePlayerTo(_current.Map.UpStairs);
    }

    /// <summary>给测试用：直接给地图和起点，不跑生成器。</summary>
    public Game(Level map, int playerX, int playerY)
    {
        _seed = 0;
        _rng = new Random(0);
        Player = new Player { X = playerX, Y = playerY };

        LevelNumber = 1;
        _current = new DungeonLevel(map);
        _levels[LevelNumber] = _current;
        _current.Sight.Update(playerX, playerY);
    }

    public void Notify(string message)
    {
        _messages.Clear();
        AddMessage(message);
    }

    private void AddMessage(string text, MessageTone tone = MessageTone.Normal)
        => _messages.Add((text, tone));

}
