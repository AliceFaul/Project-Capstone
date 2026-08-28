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
                EquipmentData oldMelee = Melee;
                
                if(oldMelee != null)
                    inventory?.AddItem(oldMelee, 1);
                
                Melee = equipment;
                Debug.Log($"Invoke Event : {oldMelee?.itemName} -> {Melee?.itemName}");
                
                OnEquipmentChanged?.Invoke(
                    new EquipmentChangedEventArgs(
                        EquipmentType.MeleeWeapon,
                        oldMelee,
                        Melee));
                break;
            case EquipmentType.RangedWeapon:
                EquipmentData oldRanged = Ranged;
                
                if(oldRanged != null)
                    inventory?.AddItem(oldRanged, 1);
                
                Ranged = equipment;
                Debug.Log($"Invoke Event : {oldRanged?.itemName} -> {Ranged?.itemName}");
                
                OnEquipmentChanged?.Invoke(
                    new EquipmentChangedEventArgs(
                        EquipmentType.RangedWeapon,
                        oldRanged,
                        Ranged));
                break;
            case EquipmentType.Armor:
                EquipmentData oldArmor = Armor;
                
                if(oldArmor != null)
                    inventory?.AddItem(oldArmor, 1);
                
                Armor = equipment;
                Debug.Log($"Invoke Event : {oldArmor?.itemName} -> {Armor?.itemName}");
                
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

                EquipmentData oldArtifact = null;
                int targetSlot = emptySlot;

                if (targetSlot < 0)
                {
                    targetSlot = 0;
                    oldArtifact = Artifacts[0];
                    if(oldArtifact != null)
                        inventory?.AddItem(oldArtifact, 1);
                }
                
                Artifacts[targetSlot] = equipment;
                
                OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(
                    EquipmentType.Artifact,
                    oldArtifact,
                    equipment));
                break;
        }
    }

    public void Unequip(EquipmentType type)
    {
        EquipmentData removed = null;

        switch (type)
        {
            case EquipmentType.MeleeWeapon:
                removed = Melee;
                Melee = null;
                break;
            case EquipmentType.RangedWeapon:
                removed = Ranged;
                Ranged = null;
                break;
            case EquipmentType.Armor:
                removed = Armor;
                Armor = null;
                break;
            case EquipmentType.Artifact:
                for (int i = Artifacts.Length - 1; i >= 0; i--)
                {
                    if (Artifacts[i] != null)
                    {
                        removed = Artifacts[i];
                        Artifacts[i] = null;
                        break;
                    }
                }
                break;
        }
        
        if(removed == null)
            return;
        
        OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(
            type,
            removed,
            null));
    }

    public void Unequip(ItemData item)
    {
        if(item.itemType != ItemType.Equipment)
            return;
        
        if(item is not EquipmentData equipment)
            return;
        
        Unequip(equipment.equipmentType);
    }
}