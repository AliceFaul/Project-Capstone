using UnityEngine;
using System.Collections.Generic;
using System;

namespace Project.Capstone.Inventory
{
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
        public List<InventorySlot> slots = new List<InventorySlot>();
        public int maxSlots;
        
        public event Action<List<InventorySlot>> OnInventoryChanged;

        public Inventory(int slotCount = 25)
        {
            maxSlots = slotCount;
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
                Debug.LogWarning($"[Inventory] Attempted to add a null Item!");
                return false;
            }
            
            bool hasAdded = false;

            if (item.isStackable)
            {
                foreach (var slot in slots)
                {
                    if (!slot.IsEmpty && slot.item == item && slot.quantity < item.maxStackSize)
                    {
                        int roomLeft = item.maxStackSize - slot.quantity;
                        int amountToAdd = Mathf.Min(quantity, roomLeft);

                        slot.quantity += amountToAdd;
                        quantity -= amountToAdd;
                        Debug.Log($"[Inventory] Adding {amountToAdd} item to {slot.item.itemName}!");
                        hasAdded = true;
                    }
                    
                    if(quantity <= 0) break;
                }
            }
            
            foreach (var slot in slots)
            {
                if(!slot.IsEmpty) continue;

                int amountToAdd = Mathf.Min(quantity, item.maxStackSize);
            
                slot.item = item;
                slot.quantity = amountToAdd;
            
                quantity -= amountToAdd;
                Debug.Log($"[Inventory] Adding {item.itemName} to empty slot!");
                hasAdded = true;
            
                if (quantity <= 0) break;
            }
            
            if(hasAdded) OnInventoryChanged?.Invoke(slots);
            else Debug.Log($"[Inventory] Inventory full!");
            return quantity <= 0;
        }

        public bool RemoveItem(ItemData item, int quantity = 1)
        {
            int totalAmount = 0;
            
            foreach (var slot in slots) if (slot.item == item) totalAmount += slot.quantity;
            if (totalAmount < quantity)
            {
                Debug.Log($"[Inventory] {item.itemName} does not have enough {quantity} items left!");
                return false;
            }

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].item == item)
                {
                    if (slots[i].quantity > quantity)
                    {
                        slots[i].quantity -= quantity;
                        quantity = 0;
                    }
                    else
                    {
                        quantity -= slots[i].quantity;
                        slots[i].Clear();
                    }

                    if (quantity <= 0) break;
                }
            }

            Debug.Log($"[Inventory] Removed {item.itemName} ({quantity}) from slots!");
            OnInventoryChanged?.Invoke(slots);
            return true;
        }
    }
}