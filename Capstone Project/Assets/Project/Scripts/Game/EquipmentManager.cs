using UnityEngine;
using System;
using System.Threading.Tasks;
using Project.Capstone.Inventory;

public class EquipmentChangedEventArgs : EventArgs
{
    public EquipSlot Slot { get; }
    public int Index { get; }
    public IInventoryItem OldItem { get; }
    public IInventoryItem NewItem { get; }

    public EquipmentChangedEventArgs(EquipSlot slot, int index, IInventoryItem oldItem, IInventoryItem newItem)
    {
        Slot = slot;
        Index = index;
        OldItem = oldItem;
        NewItem = newItem;
    }
}

public class EquipmentManager : MonoBehaviour, IManager
{
    public static EquipmentManager Instance { get; private set; }
    
    public event Action<EquipmentChangedEventArgs> OnEquipmentChanged;
    public event Action OnLoadoutReloaded;

    private PlayerDataConfig _data;
    private EquipmentProgressConfig _progressConfig;
    private bool _bound;

    public async Task<bool> Initialize()
    {
        await Task.CompletedTask;
        return EnsureBound();
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (_bound)
        {
            _data.Loadout.OnChanged -= OnChanged;
            _data.Loadout.OnReloaded -= OnReloaded;
            _bound = false;
        }
        
        if(Instance == this) Instance = null;
    }

    private bool EnsureBound()
    {
        if(_bound) return true;

        if (StartupProcessor.Instance == null || !StartupProcessor.Instance.GetService(out ConfigManager configMg))
        {
            Debug.LogError($"[EquipmentManager] Not have Config Manager. StartupProcessor/ConfigStep not load done.");
            return false;
        }

        if (!configMg.GetConfig(out _data) || !configMg.GetConfig(out _progressConfig))
        {
            Debug.LogError($"[EquipmentManager] Missing PlayerDataConfig or EquipmentProgressConfig in ConfigReferenceList.");
            return false;
        }

        _data.Loadout.OnChanged += OnChanged;
        _data.Loadout.OnReloaded += OnReloaded;
        _bound = true;
        return true;
    }

    private void OnChanged(EquipSlot slot, int index, IInventoryItem oldItem, IInventoryItem newItem)
    {
        OnEquipmentChanged?.Invoke(new EquipmentChangedEventArgs(slot, index, oldItem, newItem));
    }
    
    private void OnReloaded() => OnLoadoutReloaded?.Invoke();
    
    public EquipmentProgressConfig ProgressConfig => EnsureBound() ? _progressConfig : null;

    public bool Equip(IInventoryItem item, int artifactIndex = -1)
    {
        return EnsureBound() && _data.Loadout.TryEquip(item, artifactIndex);
    }

    public bool Unequip(EquipSlot slot, int index = 0)
    {
        return EnsureBound() && _data.Loadout.TryUnequip(slot, index);
    }

    public EquipmentInstance GetEquipped(EquipSlot slot)
    {
        if(!EnsureBound()) return null;
        
        switch(slot)
        {
            case EquipSlot.Melee: return _data.Loadout.Melee;
            case EquipSlot.Ranged: return _data.Loadout.Ranged;
            case EquipSlot.Armor: return _data.Loadout.Armor;
            default: return null;
        }
    }

    public ArtifactInstance GetArtifact(int index)
    {
        return EnsureBound() ? _data.Loadout.GetArtifact(index) : null;
    }

    public EquipmentData GetDefinition(EquipmentInstance instance)
    {
        if(instance == null || !EnsureBound()) return null;
        return _data.GetDefinition<EquipmentData>(instance.definitionId);
    }

    public float GetStat(EquipSlot slot, BonusStat stat)
    {
        var instance = GetEquipped(slot);
        var definition = GetDefinition(instance);
        return StatResolver.GetStat(instance, definition, stat, _progressConfig);
    }
}