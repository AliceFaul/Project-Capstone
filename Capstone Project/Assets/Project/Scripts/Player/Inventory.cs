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
        public Item item;
        public int quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        public InventorySlot(Item item, int quantity = 1)
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

        public bool AddItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning($"[Inventory] Attempted to add a invalid Item or Quantity!");
                return false;
            }

            if (!item.CanStack && quantity != 1)
            {
                Debug.LogWarning($"[Inventory] Non-stackable item '{item.Definition.itemName}' must be added with quantity = 1.");
                return false;
            }
            
            int remaining = quantity;

            // ===== STACKABLE ITEM =====
            if (item.CanStack)
            {
                int maxStack = Mathf.Max(1, item.Definition.maxStackSize);

                foreach (var slot in slots)
                {
                    if (slot.IsEmpty) continue;
                    if (!slot.item.CanStack) continue;
                    if (slot.item.Definition.id != item.Definition.id) continue;
                    if (slot.quantity >= maxStack) continue;
                    
                    int roomLeft = maxStack - slot.quantity;
                    int amountToAdd = Mathf.Min(remaining, roomLeft);
                    
                    slot.quantity += amountToAdd;
                    remaining -= amountToAdd;
                    
                    if(remaining <= 0) break;
                }
            }

            // ===== ADD TO EMPTY SLOT =====
            if (remaining > 0)
            {
                foreach (var slot in slots)
                {
                    if (!slot.IsEmpty) continue;

                    if (item.CanStack)
                    {
                        int maxStack = Mathf.Max(1, item.Definition.maxStackSize);

                        int amountToAdd = Mathf.Min(remaining, maxStack);

                        slot.item = item;
                        slot.quantity = amountToAdd;

                        remaining -= amountToAdd;
                    }
                    else
                    {
                        // Unique item, e.g. WeaponInstance.
                        slot.item = item;
                        slot.quantity = 1;

                        remaining = 0;
                    }

                    if (remaining <= 0) break;
                }
            }

            bool hasAdded = remaining < quantity;

            if (hasAdded)
            {
                OnInventoryChanged?.Invoke(slots);
                Debug.Log($"[Inventory] Added {quantity - remaining} x {item.Definition.itemName}!");
            }
            
            if(remaining > 0) Debug.LogWarning($"[Inventory] Inventory full! Leftover quantity: {remaining}");
            
            return remaining <= 0;
        }

        public bool RemoveItem(Item item, int quantity = 1)
        {
            if(item == null || quantity <= 0) return false;

            if (!item.CanStack)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if(slot.IsEmpty) continue;
                    if(slot.item.InstanceId != item.InstanceId) continue;
                    
                    slot.Clear();
                    OnInventoryChanged?.Invoke(slots);
                    Debug.Log($"[Inventory] Removed {item.Definition.itemName} {item.InstanceId}!");
                    return true;
                }
                
                Debug.LogWarning($"[Inventory] Instance {item.InstanceId} not found in Inventory!");
                return false;
            }
            
            int totalAmount = slots.Where(slot => !slot.IsEmpty && slot.item.CanStack && slot.item.Definition.id == item.Definition.id).Sum(slot => slot.quantity);

            if (totalAmount < quantity)
            {
                Debug.Log($"[Inventory] {item.Definition.itemName} does not have enough {quantity} items left!");
                return false;
            }

            int remaining = quantity;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];

                if(slot.IsEmpty) continue;
                if(!slot.item.CanStack) continue;
                if(slot.item.Definition.id != item.Definition.id) continue;

                if (slot.quantity > remaining)
                {
                    slot.quantity -= remaining;
                    remaining = 0;
                }
                else
                {
                    remaining -= slot.quantity;
                    slot.Clear();
                }
                
                if(remaining <= 0) break;
            }
            
            OnInventoryChanged?.Invoke(slots);
            Debug.Log($"[Inventory] Successfully removed {quantity} x {item.Definition.itemName} from inventory!");
            return true;
        }

        /*
        public List<InventorySlotData> ToData()
        {
            var data = new List<InventorySlotData>();

            foreach (var slot in slots)
            {
                data.Add(new InventorySlotData
                {
                    itemId = slot.IsEmpty ? string.Empty : slot.item.InstanceId,
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
        */
    }
}