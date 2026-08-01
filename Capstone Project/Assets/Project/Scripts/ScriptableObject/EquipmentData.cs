using UnityEngine;

public enum EquipmentType { None, MeleeWeapon, RangedWeapon, Armor, Artifact, Special }

[CreateAssetMenu(menuName = "Inventory/Equipment Data",  fileName = "New Equipment Data")]
public class EquipmentData : ItemData
{
    [Header("Equipment")]
    public EquipmentType equipmentType;
    
    public int level = 1;

    public int damageModifier;
    public int armorModifier;
    public float speedModifier;
    public float attackSpeedModifier;
    public float attackRangeModifier;
    public float critChanceModifier;
    public float critDamageModifier;

    public override void Use()
    {
        base.Use();
        EquipmentManager.Instance.Equip(this);
        RemoveFromInventory();
    }
}