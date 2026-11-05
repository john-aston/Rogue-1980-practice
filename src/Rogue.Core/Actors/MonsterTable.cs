namespace Rogue.Core;

/// <summary>原版 a–z 的怪物表。</summary>
public static class MonsterTable
{
    public static readonly MonsterDefinition[] All =
    {
        new('a', "aquator", 5, 2, 8, 1, 4, 2),
        new('b', "bat", 1, 1, 8, 1, 2, 3),
        new('c', "centaur", 4, 2, 8, 1, 6, 1),
        new('d', "dragon", 10, 4, 8, 2, 6, 5),
        new('e', "emu", 1, 1, 8, 1, 2, 0),
        new('f', "venus flytrap", 8, 3, 8, 1, 6, 2),
        new('g', "griffin", 12, 4, 8, 2, 6, 3),
        new('h', "hobgoblin", 1, 1, 8, 1, 2, 0),
        new('i', "ice monster", 2, 1, 8, 1, 3, 1),
        new('j', "jelly", 2, 2, 8, 1, 3, 0),
        new('k', "kobold", 1, 1, 8, 1, 2, 0),
        new('l', "leprechaun", 3, 2, 8, 1, 4, 0),
        new('m', "medusa", 8, 3, 8, 1, 6, 4),
        new('n', "nymph", 3, 2, 8, 1, 4, 1),
        new('o', "orc", 3, 2, 8, 1, 6, 1),
        new('p', "purple worm", 15, 5, 8, 2, 8, 4),
        new('q', "quagga", 3, 2, 8, 1, 4, 1),
        new('r', "rattlesnake", 2, 1, 8, 1, 3, 1),
        new('s', "snake", 1, 1, 8, 1, 2, 1),
        new('t', "troll", 6, 3, 8, 2, 4, 2),
        new('u', "umber hulk", 8, 3, 8, 2, 4, 4),
        new('v', "vampire", 8, 3, 8, 1, 6, 3),
        new('w', "wraith", 5, 2, 8, 1, 6, 2),
        new('x', "xeroc", 7, 3, 8, 1, 6, 3),
        new('y', "yeti", 4, 2, 8, 1, 6, 1),
        new('z', "zombie", 1, 2, 8, 1, 4, 0),
    };

    /// <summary>按地牢深度挑一只：只出现等级不超过深度的怪。</summary>
    public static MonsterDefinition Pick(int depth, Random rng)
    {
        var candidates = new List<MonsterDefinition>();

        foreach (MonsterDefinition monster in All)
        {
            if (monster.Level <= depth)
            {
                candidates.Add(monster);
            }
        }

        return candidates[rng.Next(candidates.Count)];
    }
}
