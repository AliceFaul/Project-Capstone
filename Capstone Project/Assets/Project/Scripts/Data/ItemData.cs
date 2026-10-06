using System;
using UnityEngine;

[Serializable]
public enum ItemType { QuestItem, Equipment, ConsumeItem }
[Serializable]
public enum Rarity { Common, Uncommon, Rare, Legendary }

// Tạo đường dẫn trong menu chuột phải của Unity để tạo file data mới
[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string id;               // ID dùng để quản lý mã vật phẩm
    public string itemName;         // Tên hiển thị của vật phẩm trong game
    
    [TextArea(2, 5)]
    public string itemDescription;
    
    public ItemType itemType;
    public Rarity rarity;
    
    public Sprite icon;             // Ảnh đại diện của vật phẩm trên UI (NẾU CÓ)
    public bool isStackable = true; // Bật tắt tính năng cộng dồn vật phẩm
    public int maxStackSize = 64;   // Số lượng tối đa cho phép trong một ô

    public virtual void Use()
    {
        // Use Item
        
        Debug.Log($"Using {itemName}");
    }

    protected void RemoveFromInventory()
    {
        PlayerInventory.Instance.RemoveItem(this, 1);
        Debug.Log($"Removing {itemName}");
    }
}

[System.Serializable]
public class Item
{
    public string InstanceId { get; }
    public ItemData Definition { get; }
    
    public virtual bool CanStack => Definition != null && Definition.isStackable;

    protected Item(ItemData definition)
    {
        if(definition == null) throw new System.ArgumentNullException(nameof(definition));
        InstanceId = System.Guid.NewGuid().ToString("N");
        Definition = definition;
    }
    
    public override string ToString()
    {
        return $"{Definition.itemName} [{InstanceId}]";
    }
}