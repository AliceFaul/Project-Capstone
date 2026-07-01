using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData itemData; // Loại vật phẩm trong ô
    public int stackSize;     // Số lượng vật phẩm trong ô

    public InventorySlot(ItemData item, int amount)
    {
        itemData = item;
        stackSize = amount;
    }
}

public class PlayerInventory : MonoBehaviour
{
    public List<InventorySlot> slots = new List<InventorySlot>();
    [SerializeField] private int maxSlots = 15; // Giới hạn 15 ô đồ

    public bool AddItem(ItemData item, int amount)
    {
        // Xử lý cộng dồn nếu vật phẩm cho phép xếp chồng
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.itemData == item && slot.stackSize < item.maxStackSize)
                {
                    int roomLeft = item.maxStackSize - slot.stackSize;
                    int amountToAdd = Mathf.Min(amount, roomLeft);

                    slot.stackSize += amountToAdd;
                    amount -= amountToAdd;

                    if (amount <= 0) return true; // Nhặt hết đồ, kết thúc hàm
                }
            }
        }

        // Tạo ô trống mới nếu còn dư đồ hoặc đồ không cho cộng dồn
        while (amount > 0 && slots.Count < maxSlots)
        {
            int amountToNewSlot = Mathf.Min(amount, item.maxStackSize);
            slots.Add(new InventorySlot(item, amountToNewSlot));
            amount -= amountToNewSlot;
        }

        return amount <= 0; // Trả về true nếu nhặt hết, false nếu túi đầy
    }
}