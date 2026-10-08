using System;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Item Database", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new();
    private Dictionary<string, ItemData> _itemCache;

    private void OnEnable()
    {
        Initialize();
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto rebuild cache when you add/remove item in Inspector
        Initialize();
    }
#endif

    private void Initialize()
    {
        _itemCache = new Dictionary<string, ItemData>();
        foreach (var item in items)
        {
            if(item == null) continue;

            if (string.IsNullOrWhiteSpace(item.id))
            {
                Debug.LogWarning($"[ItemDatabase] Item {item.name} has an empty ID", item);
                continue;
            }

            if (!_itemCache.TryAdd(item.id, item))
            {
                Debug.LogError($"[ItemDatabase] Duplicate Item {item.name} ID {item.id}", item);
                continue;
            }
        }
    }
    
    public ItemData Get(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (_itemCache == null) Initialize(); // Fallback lazy initialization
        
        return _itemCache.GetValueOrDefault(itemId);
    }

    public bool TryGet(string itemId, out ItemData item)
    {
        item = null;

        if(string.IsNullOrEmpty(itemId)) return false;
        if(_itemCache == null) Initialize();
        
        return _itemCache != null && _itemCache.TryGetValue(itemId, out item);
    }
}