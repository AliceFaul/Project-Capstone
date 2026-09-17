using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Item Database", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items;
    private Dictionary<string, ItemData> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<string, ItemData>();
        foreach (var item in items)
        {
            _lookup[item.id] = item;
        }
    }

    public ItemData GetCosmetic(string id)
    {
        if (_lookup == null) Initialize();
        _lookup.TryGetValue(id, out var result);
        return result;
    }
}