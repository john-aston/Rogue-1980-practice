using Rogue.Core;

namespace Rogue.Core.Tests;

public class ItemTests
{
    private static Level Arena()
        => Level.FromAscii(
            "-------",
            "|.....|",
            "|.....|",
            "|.....|",
            "-------");

    private static Game NewGame() => new(Arena(), 2, 2);

    [Fact]
    public void Stepping_on_gold_picks_it_up()
    {
        var game = NewGame();
        game.Map.AddItem(Item.Gold(17, 3, 2));

        Assert.True(game.TryMove(1, 0));

        Assert.Equal(17, game.Player.Gold);
        Assert.Empty(game.Map.Items);
    }

    [Fact]
    public void Potion_belt_is_capped_at_six()
    {
        var game = NewGame();
        game.Player.Potions = 6;
        game.Map.AddItem(Item.Potion(3, 2));

        Assert.True(game.TryMove(1, 0));

        Assert.Equal(6, game.Player.Potions);
        Assert.Single(game.Map.Items);
    }

    [Fact]
    public void Quaffing_heals_and_uses_a_potion()
    {
        var game = NewGame();
        game.Player.Hp = 1;
        game.Player.Potions = 1;

        Assert.True(game.Quaff());

        Assert.True(game.Player.Hp > 1);
        Assert.Equal(0, game.Player.Potions);
    }

    [Fact]
    public void Reading_a_scroll_teleports_and_uses_it()
    {
        var game = NewGame();
        game.Player.Scrolls = 1;

        Assert.True(game.Read());

        Assert.Equal(0, game.Player.Scrolls);
        Assert.True(game.Map.IsWalkable(game.Player.X, game.Player.Y));
    }

    [Fact]
    public void Eating_restores_food()
    {
        var game = NewGame();
        game.Player.Food = 100;
        game.Player.Rations = 1;

        Assert.True(game.Eat());

        Assert.Equal(599, game.Player.Food);
        Assert.Equal(0, game.Player.Rations);
    }

    [Fact]
    public void Buying_a_sword_upgrade_costs_gold()
    {
        var game = NewGame();
        game.Player.Gold = 100;

        Assert.True(game.Buy(1));

        Assert.Equal(60, game.Player.Gold);
        Assert.Equal(1, game.Player.SwordLevel);
    }

    [Fact]
    public void Buying_an_armor_upgrade_raises_armor()
    {
        var game = NewGame();
        game.Player.Gold = 100;
        int before = game.Player.Armor;

        Assert.True(game.Buy(2));

        Assert.Equal(60, game.Player.Gold);
        Assert.Equal(1, game.Player.ArmorLevel);
        Assert.Equal(before + 1, game.Player.Armor);
    }

    [Fact]
    public void Buying_a_potion_deducts_gold()
    {
        var game = NewGame();
        game.Player.Gold = 25;

        Assert.True(game.Buy(3));

        Assert.Equal(0, game.Player.Gold);
        Assert.Equal(1, game.Player.Potions);
    }

    [Fact]
    public void Picking_up_the_amulet_marks_it()
    {
        var game = NewGame();
        game.Map.AddItem(Item.Amulet(3, 2));

        Assert.True(game.TryMove(1, 0));

        Assert.True(game.Player.HasAmulet);
    }
}
