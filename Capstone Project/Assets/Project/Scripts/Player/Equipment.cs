using UnityEngine;
using System;

public class Equipment : MonoBehaviour
{
    public event Action OnEquipmentChanged;
    
    public WeaponData MeleeWeapon { get; private set; }
    public WeaponData RangedWeapon  { get; private set; }
    public ArmorData Armor  { get; private set; }

    public ArtifactData[] Artifacts { get; } = new ArtifactData[3];

    public void Equip(EquipmentData equipment)
    {
        switch (equipment)
        {
            case WeaponData weapon:
                EquipWeapon(weapon);
                break;
            case ArmorData armor:
                EquipArmor(armor);
                break;
            case ArtifactData artifact:
                EquipArtifact(artifact);
                break;
        }
        
        OnEquipmentChanged?.Invoke(); // Trigger event
    }
    
    private void EquipWeapon(WeaponData weapon)
    {
        switch (weapon.equipmentType)
        {
            case EquipmentType.MeleeWeapon:
                MeleeWeapon = weapon;
                break;
            case EquipmentType.RangedWeapon:
                RangedWeapon = weapon;
                break;
        }
    }

    private void EquipArmor(ArmorData armor)
    {
        Armor = armor;
    }

    private void EquipArtifact(ArtifactData artifact)
    {
        for (int i = 0; i < Artifacts.Length; i++)
        {
            if (Artifacts[i] == null)
            {
                Artifacts[i] = artifact;
                return;
            }
        }
        
        // If Artifact full, swap first artifact
        Artifacts[0] = artifact;
    }
}