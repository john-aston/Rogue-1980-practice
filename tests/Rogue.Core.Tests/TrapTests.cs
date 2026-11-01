using Rogue.Core;

namespace Rogue.Core.Tests;

public class TrapTests
{
    private static Level Arena()
        => Level.FromAscii(
            "#######",
            "#.....#",
            "#.....#",
            "#.....#",
            "#######");

    [Fact]
    public void Teleport_trap_is_consumed_when_stepped_on()
    {
        Level level = Arena();
        level.AddTrap(new Trap(TrapType.Teleport, 3, 2));
        var game = new Game(level, 2, 2);

        Assert.True(game.TryMove(1, 0));

        Assert.Empty(level.Traps);
    }

    [Fact]
    public void Trap_door_takes_the_player_to_the_next_level()
    {
        Level level = Arena();
        level.AddTrap(new Trap(TrapType.TrapDoor, 3, 2));
        var game = new Game(level, 2, 2);

        Assert.True(game.TryMove(1, 0));

        Assert.Equal(2, game.LevelNumber);
        Assert.Equal(Tiles.StairsUp, game.Map[game.Player.X, game.Player.Y]);
    }

    [Fact]
    public void Searching_can_reveal_a_hidden_trap()
    {
        Level level = Arena();
        var trap = new Trap(TrapType.Arrow, 1, 2);
        level.AddTrap(trap);
        var game = new Game(level, 2, 2);

        for (int i = 0; i < 100 && !trap.IsFound; i++)
        {
            game.Search();
        }

        Assert.True(trap.IsFound);
    }

    [Fact]
    public void Starvation_kills_the_player()
    {
        Level level = Arena();
        var game = new Game(level, 2, 2);
        game.Player.Food = -199;

        game.Wait();

        Assert.True(game.IsDead);
        Assert.Equal("You die of starvation.", game.Message);
    }

    [Fact]
    public void Hunger_warning_appears_only_once()
    {
        Level level = Arena();
        var game = new Game(level, 2, 2);
        game.Player.Food = 301;

        game.Wait();
        Assert.Equal("You are hungry.", game.Message);

        game.Wait();
        Assert.Equal("You wait.", game.Message);
    }

    [Fact]
    public void Generated_traps_sit_on_floor_tiles()
    {
        bool found = false;

        for (int seed = 0; seed < 30 && !found; seed++)
        {
            var game = new Game(seed);
            found = game.Map.Traps.Count > 0;

            foreach (Trap trap in game.Map.Traps)
            {
                Assert.True(game.Map.IsWalkable(trap.X, trap.Y));
                Assert.NotEqual(Tiles.StairsUp, game.Map[trap.X, trap.Y]);
                Assert.NotEqual(Tiles.StairsDown, game.Map[trap.X, trap.Y]);
            }
        }

        Assert.True(found);
    }
}
