
using UnityEngine;

public static class RarityColors
{
    public static Color Get(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return Color.gray;
            case Rarity.Uncommon: return Color.green;
            case Rarity.Rare: return new Color(0.2f, 0.5f, 1f); // xanh dýõng
            case Rarity.Legendary: return new Color(1f, 0.84f, 0f);  // vàng kim
            default: return Color.white;
        }
    }
}