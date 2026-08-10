using System;
using UnityEngine;

public class EquipmentChangedEventArgs : EventArgs
{
    public EquipmentType EquipmentType { get; }
    public EquipmentData OldEquipmentData { get; }
    public EquipmentData NewEquipmentData { get; }

    public EquipmentChangedEventArgs(EquipmentType equipmentType, EquipmentData oldEquipmentData,
        EquipmentData newEquipmentData)
    {
        EquipmentType = equipmentType;
        OldEquipmentData = oldEquipmentData;
        NewEquipmentData = newEquipmentData;
    }
}

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }
    
    [Header("Current Equipment")]
    public EquipmentData Melee;
    public EquipmentData Ranged;
    public EquipmentData Armor;
    public EquipmentData[] Artifacts =  new EquipmentData[3];
    
    [Space]
    
    public PlayerInventory inventory;
    public event Action<EquipmentChangedEventArgs> OnEquipmentChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        inventory = PlayerInventory.Instance;
        Debug.Log($"EquipmentManager Awake : {GetInstanceID()}");
    }

    public EquipmentData GetCurrentEquipment(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.MeleeWeapon:
                return Melee;
            case EquipmentType.RangedWeapon:
                return Ranged;
            case EquipmentType.Armor:
                return Armor;
            default:
                return null;
        }
    }

    public void Equip(ItemData item)
    {
        if(item.itemType != ItemType.Equipment)
            return;
        
        EquipmentData equipment = item as EquipmentData;
        
        if(equipment == null)
            return;

        switch (equipment.equipmentType)
        {
            case EquipmentType.MeleeWeapon:
                if (inventory != null && Melee != null)
                {
                    inventory.AddItem(Melee, 1);
                }
                else
                {
                    Debug.LogWarning("Player Inventory is missing or Melee is empty");
                }

                EquipmentData oldMelee = Melee;
                Melee = equipment;
                
                Debug.Log($"Invoke Event : {oldMelee?.itemName} -> {Melee?.itemName}");
                
                OnEquipmentChanged?.Invoke(
                    new EquipmentChangedEventArgs(
                        EquipmentType.MeleeWeapon,
                        oldMelee,
                        Melee));
                break;
            case EquipmentType.RangedWeapon:
                if (inventory != null && Ranged != null)
                {
                    inventory.AddItem(Ranged, 1);
                }
                else
                {
                    Debug.LogWarning("Player Inventory is missing or Ranged is empty");
                }
                
                EquipmentData oldRanged = Ranged;
                Ranged = equipment;
                
                Debug.Log($"Invoke Event : {oldRanged?.itemName} -> {Melee?.itemName}");
                
                OnEquipmentChanged?.Invoke(
                    new EquipmentChangedEventArgs(
                        EquipmentType.RangedWeapon,
                        oldRanged,
                        Ranged));
                break;
            case EquipmentType.Armor:
                if (inventory != null && Armor != null)
                {
                    inventory.AddItem(Armor, 1);
                }
                else
                {
                    Debug.LogWarning("Player Inventory is missing or Armor is empty");
                }
                
                EquipmentData oldArmor = Armor;
                Armor = equipment;
                
                Debug.Log($"Invoke Event : {oldArmor?.itemName} -> {Melee?.itemName}");
                
                OnEquipmentChanged?.Invoke(
                    new EquipmentChangedEventArgs(
                        EquipmentType.Armor,
                        oldArmor,
                        Armor));
                break;
            case EquipmentType.Artifact:
                int emptySlot = -1;

                for(int i = 0; i < Artifacts.Length; i++)
                {
                    if(Artifacts[i] == null)
                    {
                        emptySlot = i;
                        break;
                    }
                }

                if(emptySlot >= 0)
                {
                    Artifacts[emptySlot] = equipment;
                }
                else
                {
                    inventory.AddItem(Artifacts[0],1);
                    Artifacts[0] = equipment;
                }
                
                // TODO: Add OnEquipmentChanged event
                break;
        }
        Debug.Log($"Equip : {GetInstanceID()}");
    }

    public void Unequip(ItemData item)
    {
        if(item.itemType != ItemType.Equipment)
            return;
        
        EquipmentData equipment = item as EquipmentData;
        
        if (equipment == null)
            return;

        switch (equipment.equipmentType)
        {
            case EquipmentType.MeleeWeapon:
                Melee = null;
                break;
            case EquipmentType.RangedWeapon:
                Ranged = null;
                break;
            case EquipmentType.Armor:
                Armor = null;
                break;
            case EquipmentType.Artifact:
                for (int i = Artifacts.Length - 1; i >= 0; i--)
                {
                    if (Artifacts[i] != null)
                    {
                        Artifacts[i] = null;
                        return;
                    }
                }
                Artifacts[0] = null;
                break;
        }
    }
}