using System.Text.Json;
using Rogue.Core;

namespace Rogue.Core.Tests;

public class SaveTests
{
    [Fact]
    public void Snapshot_and_restore_keep_player_and_level_state()
    {
        var game = new Game(1234);
        game.Wait();
        game.Player.Gold = 77;
        game.Player.Potions = 3;

        SaveData data = game.Snapshot();
        Game restored = Game.Restore(data);

        Assert.Equal(game.LevelNumber, restored.LevelNumber);
        Assert.Equal(game.Turn, restored.Turn);
        Assert.Equal(game.Player.X, restored.Player.X);
        Assert.Equal(game.Player.Y, restored.Player.Y);
        Assert.Equal(game.Player.Hp, restored.Player.Hp);
        Assert.Equal(game.Player.Gold, restored.Player.Gold);
        Assert.Equal(game.Player.Potions, restored.Player.Potions);

        for (int y = 0; y < game.Map.Height; y++)
        {
            for (int x = 0; x < game.Map.Width; x++)
            {
                Assert.Equal(game.Map[x, y], restored.Map[x, y]);
            }
        }
    }

    [Fact]
    public void Save_data_survives_a_json_round_trip()
    {
        var game = new Game(42);
        game.Wait();
        game.Player.Scrolls = 2;
        game.Player.SwordLevel = 1;

        string json = JsonSerializer.Serialize(game.Snapshot());
        SaveData? data = JsonSerializer.Deserialize<SaveData>(json);

        Assert.NotNull(data);
        Game restored = Game.Restore(data!);

        Assert.Equal(2, restored.Player.Scrolls);
        Assert.Equal(1, restored.Player.SwordLevel);
    }

    [Fact]
    public void Restored_game_keeps_monsters_and_items()
    {
        var level = Level.FromAscii(
            "-------",
            "|.....|",
            "|.....|",
            "|.....|",
            "-------");

        level.AddMonster(new Monster(MonsterTable.All[0], 4, 2, 5) { IsAwake = true });
        level.AddItem(Item.Potion(3, 2));

        var game = new Game(level, 2, 2);
        Game restored = Game.Restore(game.Snapshot());

        Assert.Single(restored.Map.Monsters);
        Assert.Single(restored.Map.Items);
    }

    [Fact]
    public void Amulet_lets_the_player_win_on_level_one()
    {
        var level = Level.FromAscii(
            "-----",
            "|<..|",
            "-----");

        var game = new Game(level, 1, 1);
        game.Player.HasAmulet = true;

        Assert.False(game.TryAscend());
        Assert.True(game.IsWon);
        Assert.True(game.Score >= 1000);
    }

    [Fact]
    public void Save_store_round_trips_a_game()
    {
        string directory = Path.Combine(
            Directory.GetCurrentDirectory(),
            ".savetest-" + Guid.NewGuid().ToString("N"));

        try
        {
            var store = new SaveStore(directory);
            var game = new Game(7);
            game.Wait();

            store.Save(game.Snapshot());

            Assert.True(store.TryLoad(out SaveData? data));
            Assert.NotNull(data);

            Game restored = Game.Restore(data!);

            Assert.Equal(game.Player.X, restored.Player.X);
            Assert.Equal(game.Turn, restored.Turn);
            Assert.Equal(game.LevelNumber, restored.LevelNumber);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void Score_board_keeps_only_the_best_ten()
    {
        string directory = Path.Combine(
            Directory.GetCurrentDirectory(),
            ".scoretest-" + Guid.NewGuid().ToString("N"));

        try
        {
            var store = new SaveStore(directory);

            for (int i = 0; i < 15; i++)
            {
                store.AddScore(new ScoreEntry { Name = "player", Score = i, Level = 1 });
            }

            List<ScoreEntry> top = store.AddScore(new ScoreEntry
            {
                Name = "winner",
                Score = 100,
                Level = 1,
                Won = true,
            });

            Assert.Equal(10, top.Count);
            Assert.Equal(100, top[0].Score);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
