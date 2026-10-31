namespace Rogue.Core;

/// <summary>这一局里“哪种药水是什么颜色、哪种卷轴叫什么名字”，以及哪些类型已经鉴定过。</summary>
public sealed class ItemKnowledge
{
    private static readonly string[] PotionColors =
    {
        "red", "blue", "green", "yellow", "black", "white", "murky", "clear",
    };

    private static readonly string[] ScrollTitles =
    {
        "ZELGO MER", "JUYED AWK YACC", "NR 9",
        "XIXAXA XOXAXA XUXAXA", "PRATYAVAYAH", "DAIENVIERI TAE",
    };

    private readonly string[] _potionAppearance = new string[4];
    private readonly string[] _scrollTitle = new string[3];
    private readonly bool[] _potionsKnown = new bool[4];
    private readonly bool[] _scrollsKnown = new bool[3];

    public ItemKnowledge(Random rng)
    {
        Shuffle(PotionColors, _potionAppearance, rng);
        Shuffle(ScrollTitles, _scrollTitle, rng);
    }

    public string PotionAppearance(PotionType type) => _potionAppearance[(int)type];
    public string ScrollTitle(ScrollType type) => _scrollTitle[(int)type];

    public bool IsKnown(PotionType type) => _potionsKnown[(int)type];
    public bool IsKnown(ScrollType type) => _scrollsKnown[(int)type];

    public void Identify(PotionType type) => _potionsKnown[(int)type] = true;
    public void Identify(ScrollType type) => _scrollsKnown[(int)type] = true;

    private static void Shuffle(string[] source, string[] target, Random rng)
    {
        var copy = (string[])source.Clone();

        for (int i = copy.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }

        Array.Copy(copy, target, target.Length);
    }
}