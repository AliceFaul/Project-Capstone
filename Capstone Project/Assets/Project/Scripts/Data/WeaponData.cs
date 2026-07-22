using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponData : EquipmentData
{
    [Header("Combat Stats")]
    public int damage;
    public float attackRange;
    public float attackSpeed;
}