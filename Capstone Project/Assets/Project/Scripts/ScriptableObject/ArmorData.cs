using UnityEngine;

[CreateAssetMenu(fileName = "New Armor", menuName = "Inventory/Armor")]
public class ArmorData : EquipmentData
{
    public int armor;
    public int maxHealth;
    public float healthRegenRate;
    public float moveSpeed;
}