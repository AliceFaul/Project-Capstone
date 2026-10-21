using UnityEngine;

public enum ArmorRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

[System.Serializable]
public class ArmorItem
{
    public string armorId;
    public string armorName;
    public ArmorRarity rarity;

    public int currentLevel = 1;
    public const int MAX_LEVEL = 10;

    public int currentEXP = 0;

    // Tính lượng EXP cần thiết để lên cấp tiếp theo
    public int RequiredEXPForNextLevel => currentLevel * 100;

    // EXP nhận được khi hiến tế (Feed) món giáp này cho giáp khác
    public int BaseYieldEXP
    {
        get
        {
            int baseExp = (int)rarity * 50 + 50;
            return baseExp + (currentEXP / 2);
        }
    }

    public bool IsMaxLevel => currentLevel >= MAX_LEVEL;
}