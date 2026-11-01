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
        game.Player.Inventory.Add(Item.CreateFood(0, 0));

        SaveData data = game.Snapshot();
        Game restored = Game.Restore(data);

        Assert.Equal(game.LevelNumber, restored.LevelNumber);
        Assert.Equal(game.Turn, restored.Turn);
        Assert.Equal(game.Player.X, restored.Player.X);
        Assert.Equal(game.Player.Y, restored.Player.Y);
        Assert.Equal(game.Player.Hp, restored.Player.Hp);
        Assert.Equal(game.Player.Food, restored.Player.Food);
        Assert.Single(restored.Player.Inventory);

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
        game.Player.Inventory.Add(Item.CreatePotion(PotionType.Healing, game.Knowledge, 0, 0));
        game.Knowledge.Identify(PotionType.Healing);

        string json = JsonSerializer.Serialize(game.Snapshot());
        SaveData? data = JsonSerializer.Deserialize<SaveData>(json);

        Assert.NotNull(data);
        Game restored = Game.Restore(data!);

        Assert.True(restored.Knowledge.IsKnown(PotionType.Healing));
        Assert.Single(restored.Player.Inventory);
    }

    [Fact]
    public void Restored_game_keeps_secret_doors_monsters_items_and_traps()
    {
        var level = Level.FromAscii(
            "#######",
            "#.....#",
            "#.....#",
            "#.....#",
            "#######");

        level.AddSecretDoor(1, 1, Tiles.WallVertical);
        level.AddMonster(new Monster(MonsterTable.All[0], 4, 2, 5) { IsAwake = true });
        level.AddItem(Item.CreateFood(3, 2));
        level.AddTrap(new Trap(TrapType.Arrow, 1, 2));

        var game = new Game(level, 2, 2);
        Game restored = Game.Restore(game.Snapshot());

        Assert.True(restored.Map.IsSecretDoor(1, 1));
        Assert.Single(restored.Map.Monsters);
        Assert.Single(restored.Map.Items);
        Assert.Single(restored.Map.Traps);
    }

    [Fact]
    public void Amulet_lets_the_player_win_on_level_one()
    {
        var level = Level.FromAscii(
            "#####",
            "#<..#",
            "#####");

        var game = new Game(level, 1, 1);
        game.Player.Inventory.Add(Item.CreateAmulet(0, 0));

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
