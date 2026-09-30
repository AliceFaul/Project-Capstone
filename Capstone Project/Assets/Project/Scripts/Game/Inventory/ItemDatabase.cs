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
        foreach (var item in items) _itemCache[item.id] = item;
    }
    
    public ItemData Get(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (_itemCache == null) Initialize(); // Fallback lazy initialization
        
        return _itemCache.GetValueOrDefault(itemId);
    }
}