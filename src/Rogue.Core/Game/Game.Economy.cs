namespace Rogue.Core;

public partial class Game
{
     public bool Buy(int option)
    {
        _messages.Clear();

        return option switch
        {
            1 => BuySword(),
            2 => BuyArmor(),
            3 => BuyPotion(),
            4 => BuyRation(),
            5 => BuyMaxHp(),
            _ => BuyNothing(),
        };
    }

        private bool BuySword()
    {
        if (Player.SwordLevel >= MaxUpgradeLevel)
        {
            AddMessage("Your sword is already at its maximum.");
            return false;
        }

        int cost = UpgradeCostBase * (Player.SwordLevel + 1);

        if (Player.Gold < cost)
        {
            AddMessage($"You need {cost} gold for that.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= cost;
        Player.SwordLevel++;
        AddMessage($"Your sword is now +{Player.SwordLevel}. Gold left: {Player.Gold}.", MessageTone.Good);
        return true;
    }

    private bool BuyArmor()
    {
        if (Player.ArmorLevel >= MaxUpgradeLevel)
        {
            AddMessage("Your armor is already at its maximum.");
            return false;
        }

        int cost = UpgradeCostBase * (Player.ArmorLevel + 1);

        if (Player.Gold < cost)
        {
            AddMessage($"You need {cost} gold for that.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= cost;
        Player.ArmorLevel++;
        Player.Armor++;
        AddMessage($"Your armor is now +{Player.ArmorLevel}. Gold left: {Player.Gold}.", MessageTone.Good);
        return true;
    }

    private bool BuyPotion()
    {
        if (Player.Potions >= Player.PotionLimit)
        {
            AddMessage("Your potion belt is full.");
            return false;
        }

        if (Player.Gold < 25)
        {
            AddMessage("You need 25 gold for a potion.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= 25;
        Player.Potions++;
        AddMessage($"You buy a healing potion. Gold left: {Player.Gold}.", MessageTone.Good);
        return true;
    }

    private bool BuyRation()
    {
        if (Player.Rations >= Player.RationLimit)
        {
            AddMessage("Your pack is full of food.");
            return false;
        }

        if (Player.Gold < 15)
        {
            AddMessage("You need 15 gold for food.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= 15;
        Player.Rations++;
        AddMessage($"You buy a food ration. Gold left: {Player.Gold}.", MessageTone.Good);
        return true;
    }

    private bool BuyMaxHp()
    {
        if (Player.Gold < 60)
        {
            AddMessage("You need 60 gold for that.", MessageTone.Bad);
            return false;
        }

        Player.Gold -= 60;
        Player.MaxHp += 2;
        Player.Hp += 2;
        AddMessage($"Your maximum HP is now {Player.MaxHp}.", MessageTone.Good);
        return true;
    }

    private bool BuyNothing()
    {
        AddMessage("There is no such upgrade.");
        return false;
    }
}