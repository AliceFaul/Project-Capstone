using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [SerializeField] private Equipment playerEquipment;

    public void Equip(ItemData item)
    {
        if(item is not EquipmentData equipment)
            return;
        
        playerEquipment.Equip(equipment);
        Debug.Log(playerEquipment.MeleeWeapon.itemName);
    }
}