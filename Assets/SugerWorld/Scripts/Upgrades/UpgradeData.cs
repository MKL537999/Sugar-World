using UnityEngine;

public enum UpgradeType
{
    WeaponUpgrade,
    NewWeapon,
    StatBoost
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "SugarWorld/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName;
    public string description;
    public Sprite icon;
    public UpgradeType type;

    [Header("Weapon Upgrade")]
    public bool upgradesAllWeapons;
    public string weaponUpgradeTarget; // matches weaponName for specific weapon upgrade

    [Header("New Weapon")]
    public WeaponBase weaponPrefab;
    public int minLevel = 1;
    [System.NonSerialized] public bool alreadyUnlocked;

    [Header("Stat Boost")]
    public StatType statType;
    public float statAmount;
    public string statDisplayValue;

    public bool IsAvailable(int currentLevel)
    {
        if (type == UpgradeType.NewWeapon)
        {
            if (alreadyUnlocked) return false;
            return currentLevel >= minLevel;
        }
        return true;
    }

    public string GetDescription()
    {
        if (!string.IsNullOrEmpty(description))
            return description;

        return type switch
        {
            UpgradeType.WeaponUpgrade => upgradesAllWeapons
                ? $"All weapons +1 level"
                : $"Upgrade next weapon",
            UpgradeType.NewWeapon => weaponPrefab != null
                ? $"Gain new weapon: {weaponPrefab.weaponName}"
                : "Gain new weapon",
            UpgradeType.StatBoost => $"+{statDisplayValue} {statType}",
            _ => ""
        };
    }

    public void Apply(PlayerController player)
    {
        switch (type)
        {
            case UpgradeType.WeaponUpgrade:
                if (upgradesAllWeapons)
                {
                    for (int i = 0; i < player.WeaponCount; i++)
                        player.UpgradeWeapon(i);
                }
                else if (!string.IsNullOrEmpty(weaponUpgradeTarget))
                {
                    player.UpgradeWeaponByName(weaponUpgradeTarget);
                }
                else
                {
                    player.UpgradeRandomWeapon();
                }
                break;
            case UpgradeType.NewWeapon:
                if (weaponPrefab != null)
                {
                    player.AddWeapon(weaponPrefab);
                    alreadyUnlocked = true;
                }
                break;
            case UpgradeType.StatBoost:
                player.IncreaseStat(statType, statAmount);
                break;
        }
    }
}
