using System.Collections.Generic;
using UnityEngine;

public enum RarityType
{
    Common,
    Rare,
    Legendary
}

[System.Serializable]
public class RarityRate
{
    public RarityType rarity;
    [Range(0f, 100f)] public float dropPercentage;
    public List<ItemData> weaponPool;
}

[CreateAssetMenu(fileName = "GachaTable", menuName = "Gacha/Gacha Table")]
public class GachaTable : ScriptableObject
{
    public int gemCostPerPress = 50;
    public List<RarityRate> rarityRates = new List<RarityRate>();
}