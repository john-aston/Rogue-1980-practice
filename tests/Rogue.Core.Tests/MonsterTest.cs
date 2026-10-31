using Rogue.Core;

namespace Rogue.Core.Tests;

public class MonsterTests
{
    private static MonsterDefinition Bat
        => MonsterTable.All.First(monster => monster.Symbol == 'b');

    private static Level BuildArena()
        => Level.FromAscii(
            "#######",
            "#.....#",
            "#.....#",
            "#.....#",
            "#######");

    [Fact]
    public void Spawner_places_monsters_on_floor_but_never_on_stairs()
    {
        bool found = false;

        for (int seed = 0; seed < 20 && !found; seed++)
        {
            var game = new Game(seed);
            found = game.Map.Monsters.Count > 0;

            foreach (Monster monster in game.Map.Monsters)
            {
                Assert.True(game.Map.IsWalkable(monster.X, monster.Y));
                Assert.NotEqual(Tiles.StairsUp, game.Map[monster.X, monster.Y]);
            }
        }

        Assert.True(found);
    }

    [Fact]
    public void Attacking_an_adjacent_monster_eventually_kills_it_and_gives_experience()
    {
        Level level = BuildArena();
        level.AddMonster(new Monster(Bat, 3, 2, 5));

        var game = new Game(level, 2, 2);
        game.Player.Armor = 100;   // 让怪物打不中，只测玩家输出

        for (int i = 0; i < 100 && level.Monsters.Count > 0; i++)
        {
            game.TryMove(1, 0);
        }

        Assert.Empty(level.Monsters);
        Assert.True(game.Player.Experience > 0);
    }

    [Fact]
    public void An_awake_monster_steps_toward_the_player()
    {
        Level level = BuildArena();
        var monster = new Monster(Bat, 5, 2, 8) { IsAwake = true };
        level.AddMonster(monster);

        var game = new Game(level, 2, 2);
        int before = Math.Abs(monster.X - game.Player.X) + Math.Abs(monster.Y - game.Player.Y);

        game.Wait();

        int after = Math.Abs(monster.X - game.Player.X) + Math.Abs(monster.Y - game.Player.Y);
        Assert.True(after < before);
    }

    [Fact]
    public void Waiting_beside_a_monster_can_kill_the_player()
    {
        Level level = BuildArena();
        level.AddMonster(new Monster(Bat, 3, 2, 8));

        var game = new Game(level, 2, 2);
        game.Player.Hp = 1;
        game.Player.Armor = 0;

        for (int i = 0; i < 200 && !game.IsDead; i++)
        {
            game.Wait();
        }

        Assert.True(game.IsDead);
    }

    [Fact]
    public void Experience_raises_the_player_level()
    {
        Level level = BuildArena();
        level.AddMonster(new Monster(Bat, 3, 2, 3));

        var game = new Game(level, 2, 2);
        game.Player.Armor = 100;
        game.Player.Experience = 4;   // 差一点升级

        for (int i = 0; i < 100 && level.Monsters.Count > 0; i++)
        {
            game.TryMove(1, 0);
        }

        Assert.Equal(2, game.Player.Level);
        Assert.True(game.Player.MaxHp > 12);
    }
}