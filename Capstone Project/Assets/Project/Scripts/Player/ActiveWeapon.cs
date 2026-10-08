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

    private GameObject _currentMeleeVisual;
    private GameObject _currentRangedVisual;
    private int _meleeRequestId = 0;
    private int _rangedRequestId = 0;

    private PlayerDataConfig _config;
    private Loadout _loadout;
    private ItemDatabase _itemDatabase;
    
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
        _meleeRequestId++;
        _rangedRequestId++;
        DestroyVisual(ref _currentMeleeVisual);
        DestroyVisual(ref _currentRangedVisual);
    }

    private async Task Initialize()
    {
        _config = StartupProcessor.Instance?.GetService<ConfigManager>().GetConfig<PlayerDataConfig>();

        if (_config == null)
        {
            Debug.LogWarning($"[ActiveWeapon] PlayerDataConfig not found.");
            return;
        }
        
        _loadout = _config.Loadout;
        _itemDatabase = _config.ItemDatabase;
        
        if(_loadout == null || _itemDatabase == null) return;
        
        _loadout.OnChanged += OnLoadoutChanged;
        _loadout.OnReloaded += OnLoadoutReloaded;

        await UpdateMeleeWeapon(_loadout.Melee);
        await UpdateRangedWeapon(_loadout.Ranged);
    }

    private async void OnLoadoutChanged(EquipSlot slot, int index, IInventoryItem oldItem, IInventoryItem newItem)
    {
        try
        {
            switch (slot)
            {
                case EquipSlot.Melee:
                    await UpdateMeleeWeapon(newItem as EquipmentInstance);
                    break;
                case EquipSlot.Ranged:
                    await UpdateRangedWeapon(newItem as EquipmentInstance);
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
            await UpdateMeleeWeapon(_loadout.Melee);
            await UpdateRangedWeapon(_loadout.Ranged);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task UpdateMeleeWeapon(EquipmentInstance weapon)
    {
        int requestId = ++_meleeRequestId;

        if (!TryGetDefinition(weapon, EquipmentType.MeleeWeapon, out var definition))
        {
            DestroyVisual(ref _currentMeleeVisual);
            return;
        }
        
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

    private async Task UpdateRangedWeapon(EquipmentInstance weapon)
    {
        int requestId = ++_rangedRequestId;

        if (!TryGetDefinition(weapon, EquipmentType.RangedWeapon, out var definition))
        {
            DestroyVisual(ref _currentRangedVisual);
            return;
        }
        
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

    private bool TryGetDefinition(EquipmentInstance instance, EquipmentType type, out EquipmentData definition)
    {
        definition = null;
        
        if(instance == null) return false;
        if(_itemDatabase == null) return false;

        if (!_itemDatabase.TryGet(instance.definitionId, out var item))
        {
            Debug.LogWarning($"[ActiveWeapon] Definition not found: {instance.definitionId}");
            return false;
        }
        
        definition = item as EquipmentData;

        if (definition == null)
        {
            Debug.LogWarning($"[ActiveWeapon] Item '{instance.definitionId}' is not EquipmentData.'");
            return false;
        }

        if (definition.equipmentType != type)
        {
            Debug.LogWarning($"[ActiveWeapon] Invalid equipment type. Expected: {type}, Actual: {definition.equipmentType}.");
            definition = null;
            return false;
        }
        
        return true;
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