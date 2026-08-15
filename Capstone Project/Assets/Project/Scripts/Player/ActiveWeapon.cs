using System;
using UnityEngine;
using System.Threading.Tasks;

public class ActiveWeapon : MonoBehaviour
{
    [Header("Melee Weapon")]
    [Tooltip("Socket to hold current prefab melee weapon")]
    [SerializeField] private Transform meleeSocket;
    
    [Header("Ranged Weapon")]
    [Tooltip("Socket to hold current prefab ranged weapon")]
    [SerializeField] private Transform rangedSocket;

    private GameObject _currentMeleeVisual;
    private GameObject _currentRangedVisual;
    private int _meleeRequestId = 0;
    private int _rangedRequestId = 0;
    
    private async void Start()
    {
        try
        {
            await UpdateMeleeWeapon(EquipmentManager.Instance.Melee);
            await UpdateRangedWeapon(EquipmentManager.Instance.Ranged);
            EquipmentManager.Instance.OnEquipmentChanged += UpdateWeapons;
        }
        catch (Exception e)
        {
            Debug.LogError("[ActiveWeapon.Start()] Update Weapons Error: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= UpdateWeapons;
    }

    private async void UpdateWeapons(EquipmentChangedEventArgs args)
    {
        try
        {
            Debug.Log($"Receive Event : {args.EquipmentType}");
        
            switch (args.EquipmentType)
            {
                case EquipmentType.MeleeWeapon:
                    await UpdateMeleeWeapon(args.NewEquipmentData);
                    break;
                case EquipmentType.RangedWeapon:
                    await UpdateRangedWeapon(args.NewEquipmentData);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[ActiveWeapon.UpdateWeapons()] Update Weapons Error: " + e.Message);
        }
    }

    private async Task UpdateMeleeWeapon(EquipmentData equipment)
    {
        if (equipment == null || equipment.equipmentType != EquipmentType.MeleeWeapon)
            return;
        
        int requestId = ++_meleeRequestId;
        GameObject newWeapon = await WeaponFactory.Create(equipment, meleeSocket);

        if (requestId != _meleeRequestId)
        {
            WeaponFactory.DestroyInstance(newWeapon);
            return;
        }
        
        WeaponFactory.DestroyInstance(_currentMeleeVisual);
        _currentMeleeVisual = newWeapon;

        Debug.Log($"Change melee weapon: {equipment.itemName}");
    }

    private async Task UpdateRangedWeapon(EquipmentData equipment)
    {
        if (equipment == null || equipment.equipmentType != EquipmentType.RangedWeapon)
            return;
        
        int requestId = ++_rangedRequestId;
        GameObject newWeapon = await WeaponFactory.Create(equipment, rangedSocket);

        if (requestId != _rangedRequestId)
        {
            WeaponFactory.DestroyInstance(newWeapon);
            return;
        }
        
        WeaponFactory.DestroyInstance(_currentRangedVisual);
        _currentRangedVisual = newWeapon;
        
        Debug.Log($"Change ranged weapon: {equipment.itemName}");
    }
    
    // === HELPER SHOW AND HIDE WEAPON
    public void ShowMelee() => _currentMeleeVisual?.SetActive(true);
    public void ShowRanged() => _currentRangedVisual?.SetActive(true);
    public void HideMelee() => _currentMeleeVisual?.SetActive(false);
    public void HideRanged() => _currentRangedVisual?.SetActive(false);
    
    // === HELPER SWORD VFX ===
    public void ActivateTrail() => _currentMeleeVisual?.GetComponent<TrailVFXHandler>().PlayTrail();
    public void DeactivateTrail() => _currentMeleeVisual?.GetComponent<TrailVFXHandler>().StopTrail();
}