using System;
using UnityEngine;

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
    public event Action OnEquipmentChanged;

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
                Melee = equipment;
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
                Ranged = equipment;
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
                Armor = equipment;
                break;
            case EquipmentType.Artifact:
                if (inventory != null)
                {
                    for (int i = 0; i < Artifacts.Length; i++)
                    {
                        if (Artifacts[i] != null)
                        {
                            inventory.AddItem(Artifacts[i], 1);
                            return;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Player Inventory is missing");
                }
                
                for (int i = 0; i < Artifacts.Length; i++)
                {
                    if (Artifacts[i] == null)
                    {
                        Artifacts[i] = equipment;
                        return;
                    }
                }
                Artifacts[0] = equipment;
                break;
        }
        
        OnEquipmentChanged?.Invoke();
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
                    if (Artifacts[i] == null)
                    {
                        Artifacts[i] = null;
                        return;
                    }
                }
                Artifacts[0] = null;
                break;
        }
        
        OnEquipmentChanged?.Invoke();
    }
}