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

    public ItemKnowledge(Random rng)//开局把颜色/卷轴标题洗牌，决定本局哪种药水是什么颜色
    {
        Shuffle(PotionColors, _potionAppearance, rng);
        Shuffle(ScrollTitles, _scrollTitle, rng);
    }

    private ItemKnowledge()
    {
    }

    public KnowledgeData Snapshot() => new KnowledgeData
    {
        PotionAppearances = (string[])_potionAppearance.Clone(),
        ScrollTitles = (string[])_scrollTitle.Clone(),
        PotionsKnown = (bool[])_potionsKnown.Clone(),
        ScrollsKnown = (bool[])_scrollsKnown.Clone(),
    };

    public static ItemKnowledge Restore(KnowledgeData data)
    {
        var knowledge = new ItemKnowledge();

        Copy(data.PotionAppearances, knowledge._potionAppearance);
        Copy(data.ScrollTitles, knowledge._scrollTitle);
        Copy(data.PotionsKnown, knowledge._potionsKnown);
        Copy(data.ScrollsKnown, knowledge._scrollsKnown);

        return knowledge;
    }

    private static void Copy<T>(T[] source, T[] target)
    {
        Array.Copy(source, target, Math.Min(source.Length, target.Length));
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