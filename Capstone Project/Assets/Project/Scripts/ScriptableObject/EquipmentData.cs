using UnityEngine;

public enum EquipmentType { None, MeleeWeapon, RangedWeapon, Armor, Artifact, Special }

public enum Rarity { Common, Uncommon, Rare, Legendary }

public abstract class EquipmentData : ItemData
{
    [Header("Equipment")]
    public EquipmentType equipmentType;
    public Rarity rarity;
    public int level = 1;
}