using System;
using UnityEngine;
using System.Threading.Tasks;
using Project.Capstone.Inventory;

public class ActiveWeapon : MonoBehaviour
{
    [Header("Melee Weapon")]
    [Tooltip("Socket to hold current prefab melee weapon")]
    [SerializeField] private Transform meleeSocket;
    
    [Header("Ranged Weapon")]
    [Tooltip("Socket to hold current prefab ranged weapon")]
    [SerializeField] private Transform rangedSocket;

    private EquipmentManager _equipmentManager;
    
    private GameObject _currentMeleeVisual;
    private GameObject _currentRangedVisual;
    private int _meleeRequestId = 0;
    private int _rangedRequestId = 0;
    
    private async void Start()
    {
        try
        {
            await Initialize();
        }
        catch (Exception e)
        {
            Debug.LogError("[ActiveWeapon.Start()] Update Weapons Error: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
            _equipmentManager.OnLoadoutReloaded -= OnLoadoutReloaded;
        }
        
        _meleeRequestId++;
        _rangedRequestId++;
        DestroyVisual(ref _currentMeleeVisual);
        DestroyVisual(ref _currentRangedVisual);
    }

    private async Task Initialize()
    {
        while(EquipmentManager.Instance == null) await Task.Yield();
        if(this == null) return;
        
        _equipmentManager = EquipmentManager.Instance;
        _equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
        _equipmentManager.OnLoadoutReloaded += OnLoadoutReloaded;

        await UpdateMeleeWeapon(_equipmentManager.GetEquipped(EquipSlot.Melee));
        await UpdateRangedWeapon(_equipmentManager.GetEquipped(EquipSlot.Ranged));
    }

    private async void OnEquipmentChanged(EquipmentChangedEventArgs args)
    {
        try
        {
            switch (args.Slot)
            {
                case EquipSlot.Melee:
                    await UpdateMeleeWeapon(args.NewItem as EquipmentInstance);
                    break;
                case EquipSlot.Ranged:
                    await UpdateRangedWeapon(args.NewItem as EquipmentInstance);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async void OnLoadoutReloaded()
    {
        try
        {
            await UpdateMeleeWeapon(_equipmentManager.GetEquipped(EquipSlot.Melee));
            await UpdateRangedWeapon(_equipmentManager.GetEquipped(EquipSlot.Ranged));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task UpdateMeleeWeapon(EquipmentInstance instance)
    {
        int requestId = ++_meleeRequestId;
        DestroyVisual(ref _currentMeleeVisual);
        
        if(instance == null) return;
        EquipmentData definition = _equipmentManager.GetDefinition(instance);
        
        if(definition == null) return;
        GameObject newWeapon = await WeaponFactory.Create(definition, meleeSocket);

        if (requestId != _meleeRequestId)
        {
            WeaponFactory.DestroyInstance(newWeapon);
            return;
        }

        if (newWeapon == null)
        {
            DestroyVisual(ref _currentMeleeVisual);
            return;
        }
        
        WeaponFactory.DestroyInstance(_currentMeleeVisual);
        _currentMeleeVisual = newWeapon;

        Debug.Log($"Change melee weapon: {definition.itemName}");
    }

    private async Task UpdateRangedWeapon(EquipmentInstance instance)
    {
        int requestId = ++_rangedRequestId;
        DestroyVisual(ref _currentRangedVisual);
        
        if(instance == null) return;
        EquipmentData definition = _equipmentManager.GetDefinition(instance);
        
        if(definition == null) return;
        GameObject newWeapon = await WeaponFactory.Create(definition, rangedSocket);

        if (requestId != _rangedRequestId)
        {
            WeaponFactory.DestroyInstance(newWeapon);
            return;
        }

        if (newWeapon == null)
        {
            DestroyVisual(ref _currentRangedVisual);
            return;
        }
        
        WeaponFactory.DestroyInstance(_currentRangedVisual);
        _currentRangedVisual = newWeapon;
        
        Debug.Log($"Change ranged weapon: {definition.itemName}");
    }

    private static void DestroyVisual(ref GameObject visual)
    {
        if(visual == null) return;
        WeaponFactory.DestroyInstance(visual);
        visual = null;
    }
    
    // === HELPER SHOW AND HIDE WEAPON
    public void ShowMelee() => meleeSocket.gameObject.SetActive(true);
    public void ShowRanged() => rangedSocket.gameObject.SetActive(true);
    public void HideMelee() => meleeSocket.gameObject.SetActive(false);
    public void HideRanged() => rangedSocket.gameObject.SetActive(false);
    
    // === HELPER SWORD VFX ===
    public void ActivateTrail()
    {
        if(_currentMeleeVisual == null) return;
        var trail = _currentMeleeVisual.GetComponent<TrailVFXHandler>();
        if(trail != null) trail.PlayTrail();
    }

    public void DeactivateTrail()
    {
        if(_currentMeleeVisual == null) return;
        var trail = _currentMeleeVisual.GetComponent<TrailVFXHandler>();
        if(trail != null) trail.StopTrail();
    }
}