using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EquipmentChangedEventArgs : EventArgs
{
    public EquipmentType EquipmentType { get; }
    public Item OldItem { get; }
    public Item NewItem { get; }

    public EquipmentChangedEventArgs(EquipmentType equipmentType, Item oldItem, Item newItem)
    {
        EquipmentType = equipmentType;
        OldItem = oldItem;
        NewItem = newItem;
    }
}

public class EquipmentManager : MonoBehaviour, IManager
{
    public static EquipmentManager Instance { get; private set; }
    
    [Header("Current Equipment")]
    public Weapon Melee { get; private set; }
    public Weapon Ranged { get; private set; }
    public Armor Armor { get; private set; }

    [SerializeField] private int artifactSlotCount = 3;
    
    public event Action<EquipmentChangedEventArgs> OnEquipmentChanged;

    public async Task<bool> Initialize()
    {
        // TODO: Add save/load equipment in PlayerConfigData script
        // TODO: Reference inventory
        
        await Task.CompletedTask;
        return true;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);  
            return;
        }

        Instance = this;

        artifactSlotCount = Mathf.Max(1, artifactSlotCount);
    }

    public Item GetCurrentEquipment(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.MeleeWeapon => Melee,
            EquipmentType.RangedWeapon => Ranged,
            EquipmentType.Armor => Armor,
            _ => null
        };
    }

    public void Equip(Item item)
    {
        if(item == null) return;
        
        switch (item)
        {
            case Weapon weapon:
                EquipWeapon(weapon);
                break;
            case Armor armor:
                EquipArmor(armor);
                break;
            default:
                Debug.LogWarning($"[EquipmentManager] Unsupported item type: {item.GetType().Name}.");
                break;
        }
    }

    private void EquipWeapon(Weapon weapon)
    {
        switch (weapon.WeaponDefinition.equipmentType)
        {
            case EquipmentType.MeleeWeapon:
                Weapon oldMelee = Melee;
                Melee = weapon;
                OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(EquipmentType.MeleeWeapon, oldMelee, weapon));
                break;
            case EquipmentType.RangedWeapon:
                Weapon oldRanged = Ranged;
                Ranged = weapon;
                OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(EquipmentType.RangedWeapon, oldRanged, weapon));
                break;
            default:
                Debug.LogWarning($"[EquipmentManager] Weapon '{weapon.Definition.itemName}' has invalid type: {weapon.WeaponDefinition.equipmentType}");
                break;
        }
    }

    private void EquipArmor(Armor armor)
    {
        Armor oldArmor = Armor;
        Armor = armor;
        OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(EquipmentType.Armor, oldArmor, armor));
    }

    public void Unequip(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.MeleeWeapon:
            {
                if (Melee == null) return;

                Weapon old = Melee;
                Melee = null;
                OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(EquipmentType.MeleeWeapon, old, null));
                break;
            }

            case EquipmentType.RangedWeapon:
            {
                if (Ranged == null) return;

                Weapon old = Ranged;
                Ranged = null;
                OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(EquipmentType.RangedWeapon, old, null));
                break;
            }

            case EquipmentType.Armor:
            {
                if (Armor == null) return;

                Armor old = Armor;
                Armor = null;
                OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(EquipmentType.Armor, old, null));
                break;
            }
        }
    }

    public void Unequip(Item item)
    {
        if(item == null) return;

        switch (item)
        {
            case Weapon weapon:
                UnequipWeapon(weapon);
                break;
            case Armor armor:
                if(Armor?.InstanceId == armor.InstanceId) Unequip(armor);
                break;
        }
    }

    private void UnequipWeapon(Weapon weapon)
    {
        if (weapon.WeaponDefinition.equipmentType == EquipmentType.MeleeWeapon && Melee?.InstanceId == weapon.InstanceId)
        {
            Unequip(EquipmentType.MeleeWeapon);
        }
        else if (weapon.WeaponDefinition.equipmentType == EquipmentType.RangedWeapon && Ranged?.InstanceId == weapon.InstanceId)
        {
            Unequip(EquipmentType.RangedWeapon);
        }
    }
}