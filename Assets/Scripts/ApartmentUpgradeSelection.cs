using UnityEngine;

public enum ApartmentUpgradeChoice
{
    None = 0,
    UsedTv,
    HighSpeedInternet,
    Decluttering
}

public static class ApartmentUpgradeSelection
{
    public static ApartmentUpgradeChoice SelectedChoice { get; private set; } = ApartmentUpgradeChoice.None;

    private static bool _startBonusApplied;

    public static void ResetForNewRun()
    {
        SelectedChoice = ApartmentUpgradeChoice.None;
        _startBonusApplied = false;
    }

    public static void Select(ApartmentUpgradeChoice choice)
    {
        SelectedChoice = choice;
    }

    public static int GetRentIncrease()
    {
        switch (SelectedChoice)
        {
            case ApartmentUpgradeChoice.UsedTv: return 200;
            case ApartmentUpgradeChoice.HighSpeedInternet: return 150;
            default: return 0;
        }
    }

    public static void ApplyStartOfGameBonus(ResourceBank bank, Resource moneyResource, Resource energyResource)
    {
        if (_startBonusApplied || bank == null) return;
        _startBonusApplied = true;

        if (SelectedChoice != ApartmentUpgradeChoice.Decluttering) return;

        if (energyResource != null)
            bank.Add(energyResource, -20);
        if (moneyResource != null)
            bank.Add(moneyResource, 75);
    }

    public static void ApplyRoundBonus(ResourceBank bank, Resource energyResource, Resource networkResource)
    {
        if (bank == null) return;

        switch (SelectedChoice)
        {
            case ApartmentUpgradeChoice.UsedTv:
                if (energyResource != null)
                    bank.Add(energyResource, 50);
                break;
            case ApartmentUpgradeChoice.HighSpeedInternet:
                if (networkResource != null)
                    bank.Add(networkResource, 1);
                break;
        }
    }
}
