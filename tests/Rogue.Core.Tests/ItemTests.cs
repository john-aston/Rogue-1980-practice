using Rogue.Core;

namespace Rogue.Core.Tests;

public class ItemTests
{
    private static Game NewGame()
    {
        var level = Level.FromAscii(
            "#######",
            "#.....#",
            "#.....#",
            "#.....#",
            "#######");

        return new Game(level, 2, 2);
    }

    [Fact]
    public void Potion_appearance_is_stable_for_the_same_game()
    {
        var game = NewGame();

        Item first = Item.CreatePotion(PotionType.Healing, game.Knowledge, 0, 0);
        Item second = Item.CreatePotion(PotionType.Healing, game.Knowledge, 0, 0);

        Assert.Equal(first.DisplayName(game.Knowledge), second.DisplayName(game.Knowledge));
    }

    [Fact]
    public void Quaffing_a_potion_identifies_its_type()
    {
        var game = NewGame();
        game.Player.Hp = 1;
        game.Player.Inventory.Add(Item.CreatePotion(PotionType.Healing, game.Knowledge, 0, 0));

        Assert.True(game.Quaff(0));

        Assert.True(game.Player.Hp > 1);
        Assert.True(game.Player.Hp <= game.Player.MaxHp);
        Assert.True(game.Knowledge.IsKnown(PotionType.Healing));
    }

    [Fact]
    public void Scroll_of_identify_reveals_an_unknown_potion()
    {
        var game = NewGame();
        game.Player.Inventory.Add(Item.CreateScroll(ScrollType.Identify, game.Knowledge, 0, 0));
        game.Player.Inventory.Add(Item.CreatePotion(PotionType.Poison, game.Knowledge, 0, 0));

        Assert.True(game.Read(0));

        Assert.True(game.Knowledge.IsKnown(PotionType.Poison));
    }

    [Fact]
    public void Wielding_a_weapon_moves_it_out_of_the_pack()
    {
        var game = NewGame();
        var sword = new Item(ItemKind.Weapon, ')', "long sword", 0, 0)
        {
            DamageDice = 2,
            DamageSides = 4,
        };
        game.Player.Inventory.Add(sword);

        Assert.True(game.Wield(0));

        Assert.Same(sword, game.Player.Weapon);
        Assert.Empty(game.Player.Inventory);
    }

    [Fact]
    public void Wearing_armor_changes_armor_class_and_taking_it_off_reverts_it()
    {
        var game = NewGame();
        var armor = new Item(ItemKind.Armor, '[', "chain mail", 0, 0) { Armor = 4 };
        game.Player.Inventory.Add(armor);

        Assert.True(game.Wear(0));
        Assert.Equal(8, game.Player.Armor);

        Assert.True(game.TakeOff());
        Assert.Equal(4, game.Player.Armor);
        Assert.Single(game.Player.Inventory);
    }

    [Fact]
    public void Stepping_on_gold_picks_it_up()
    {
        var game = NewGame();
        game.Map.AddItem(Item.CreateGold(17, 3, 2));

        Assert.True(game.TryMove(1, 0));

        Assert.Equal(17, game.Player.Gold);
        Assert.Empty(game.Map.Items);
    }

    [Fact]
    public void Picking_up_an_item_moves_it_into_the_pack()
    {
        var game = NewGame();
        game.Map.AddItem(Item.CreateFood(2, 2));

        Assert.True(game.PickUp());

        Assert.Single(game.Player.Inventory);
        Assert.Empty(game.Map.Items);
    }

    [Fact]
    public void Dropping_an_item_puts_it_back_on_the_floor()
    {
        var game = NewGame();
        game.Player.Inventory.Add(Item.CreateFood(0, 0));

        Assert.True(game.Drop(0));

        Assert.Empty(game.Player.Inventory);
        Assert.Single(game.Map.Items);
        Assert.Equal(2, game.Map.Items[0].X);
        Assert.Equal(2, game.Map.Items[0].Y);
    }

    [Fact]
    public void Magic_mapping_marks_the_whole_level_as_seen()
    {
        var game = NewGame();
        game.Player.Inventory.Add(Item.CreateScroll(ScrollType.MagicMapping, game.Knowledge, 0, 0));

        Assert.True(game.Read(0));

        Assert.True(game.Sight.IsSeen(0, 0));
        Assert.True(game.Sight.IsSeen(5, 3));
    }
}