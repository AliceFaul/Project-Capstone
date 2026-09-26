using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Project.Capstone.Inventory
{
    [Serializable]
    public class InventorySlotData
    {
        public string itemId;
        public int quantity;
    }
    
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;
        public bool IsEmpty => item == null || quantity == 0;

        public InventorySlot(ItemData item, int quantity = 1)
        {
            this.item = item;
            this.quantity = quantity;
        }
        
        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }
    
    [System.Serializable]
    public class Inventory
    {
        public List<InventorySlot> slots;
        public int maxSlots;
        
        public event Action<List<InventorySlot>> OnInventoryChanged;

        public Inventory(int slotCount = 25)
        {
            maxSlots = slotCount;
            slots = new List<InventorySlot>(maxSlots);
            slots.Clear();

            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new InventorySlot(null, 0));
            }
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null)
            {
                Debug.LogWarning($"[Inventory] Attempted to add a invalid Item or Quantity!");
                return false;
            }
            
            int remaining = quantity;
            int maxStack = item.isStackable ? Mathf.Max(1, item.maxStackSize) : 1;
            
            // Add to slots have an item
            if (item.isStackable)
            {
                foreach (var slot in slots)
                {
                    if (!slot.IsEmpty && slot.item.id == item.id && slot.quantity < maxStack)
                    {
                        int roomLeft = maxStack - slot.quantity;
                        int amountToAdd = Mathf.Min(remaining, roomLeft);

                        slot.quantity += amountToAdd;
                        remaining -= amountToAdd;
                        
                        if(remaining <= 0) break;
                    }
                }
            }
            
            // Add to empty slots
            if (remaining > 0)
            {
                foreach (var slot in slots)
                {
                    if (!slot.IsEmpty) continue;

                    int amountToAdd = Mathf.Min(remaining, maxStack);

                    slot.item = item;
                    slot.quantity = amountToAdd;
                    remaining -= amountToAdd;
                    
                    if (quantity <= 0) break;
                }
            }

            bool hasAdded = remaining < quantity;

            if (hasAdded)
            {
                OnInventoryChanged?.Invoke(slots);
                Debug.Log($"[Inventory] Added {item.itemName} ({quantity}) to slots!");
            }
            
            if(remaining > 0) Debug.LogWarning($"[Inventory] Inventory full! Leftover quantity: {remaining}");
            
            return remaining <= 0;
        }

        public bool RemoveItem(ItemData item, int quantity = 1)
        {
            if(item == null || quantity <= 0) return false;
            int totalAmount = slots.Where(slot => !slot.IsEmpty && slot.item.id == item.id).Sum(slot => slot.quantity);

            if (totalAmount < quantity)
            {
                Debug.Log($"[Inventory] {item.itemName} does not have enough {quantity} items left!");
                return false;
            }

            int remaining = quantity;
            
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (!slots[i].IsEmpty && slots[i].item.id == item.id)
                {
                    if (slots[i].quantity > remaining)
                    {
                        slots[i].quantity -= remaining;
                        remaining = 0;
                    }
                    else
                    {
                        remaining -= slots[i].quantity;
                        slots[i].Clear();
                    }

                    if (remaining <= 0) break;
                }
            }

            Debug.Log($"[Inventory] Successfully removed {quantity} x {item.itemName} from inventory!");
            OnInventoryChanged?.Invoke(slots);
            return true;
        }

        public List<InventorySlotData> ToData()
        {
            var data = new List<InventorySlotData>();

            foreach (var slot in slots)
            {
                data.Add(new InventorySlotData
                {
                    itemId = slot.IsEmpty ? string.Empty : slot.item.id,
                    quantity = slot.quantity
                });
            }
            
            return data;
        }

        public void ApplyData(List<InventorySlotData> data, Func<string, ItemData> itemLookup)
        {
            if(data == null) return;

            for (int i = 0; i < maxSlots; i++)
            {
                if (i < data.Count && !string.IsNullOrEmpty(data[i].itemId))
                {
                    ItemData loadedItem = itemLookup?.Invoke(data[i].itemId);
                    slots[i].item = loadedItem;
                    slots[i].quantity = loadedItem != null ? data[i].quantity : 0;
                }
                else
                {
                    slots[i].Clear();
                }
            }
            
            OnInventoryChanged?.Invoke(slots);
        }
    }
}