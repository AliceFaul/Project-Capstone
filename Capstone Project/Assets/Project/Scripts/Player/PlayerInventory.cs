using System;
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
    public static PlayerInventory Instance  { get; private set; }
    
    public List<InventorySlot> slots = new List<InventorySlot>();
    [SerializeField] private int maxSlots = 15; // Giới hạn 15 ô đồ

    public GameObject itemPickupPrefab; // Vật thể mẫu để sinh ra khi thả đồ

    //EVENT ĐỂ CÁC SCRIPT KHÁC NHẬN BIẾT INVENTORY THAY ĐỔI
    public static event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool AddItem(ItemData item, int amount)
    {
        bool hasAdded = false;

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
                    hasAdded = true;

                    if (amount <= 0)
                    {
                        // active Event khi cộng dồn thành công và nhặt hết đồ
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }

        // Tạo ô trống mới nếu còn dư đồ hoặc đồ không cho cộng dồn
        while (amount > 0 && slots.Count < maxSlots)
        {
            int amountToNewSlot = Mathf.Min(amount, item.maxStackSize);
            slots.Add(new InventorySlot(item, amountToNewSlot));
            amount -= amountToNewSlot;
            hasAdded = true;
        }

        // Kích hoạt Event nếu có bất kỳ ô mới nào được thêm vào thành công
        if (hasAdded) OnInventoryChanged?.Invoke();

        return amount <= 0; // Trả về true nếu nhặt hết, false nếu túi đầy
    }

    // Hàm xóa bớt hoặc trừ số lượng vật phẩm trong kho đồ
    public bool RemoveItem(ItemData item, int amount)
    {
        // Kiểm tra xem trong kho đồ có đủ số lượng để trừ không trước khi làm
        int totalAmount = 0;
        foreach (var slot in slots)
        {
            if (slot.itemData == item) totalAmount += slot.stackSize;
        }

        if (totalAmount < amount) return false; // Không đủ đồ để xóa, hủy lệnh

        // Tiến hành trừ số lượng (quét từ cuối danh sách lên để ưu tiên trừ ô lẻ trước)
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].itemData == item)
            {
                if (slots[i].stackSize > amount)
                {
                    slots[i].stackSize -= amount;
                    amount = 0;
                }
                else
                {
                    amount -= slots[i].stackSize;
                    slots.RemoveAt(i); // Ô đồ trống hoàn toàn thì xóa ô đó khỏi List
                }

                if (amount <= 0) break; // Đã trừ đủ số lượng cần xóa
            }
        }

        // Kích hoạt Event sau khi đã xóa/giảm số lượng vật phẩm thành công
        OnInventoryChanged?.Invoke();
        return true;
    }

    // Hàm thả vật phẩm ra môi trường thế giới 3D
    public void DropItem(ItemData item, int amount)
    {
        // Gọi hàm RemoveItem để trừ số lượng trong kho đồ trước 
        // (Hàm RemoveItem chạy thành công đã tự kích hoạt OnInventoryChanged ở trên rồi)
        if (RemoveItem(item, amount))
        {
            // Tính toán vị trí thả đồ trước mặt Player
            Vector3 dropPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

            // Sinh ra khối đồ ngoài môi trường
            GameObject droppedObj = Instantiate(itemPickupPrefab, dropPosition, Quaternion.identity);

            // Nạp dữ liệu vật phẩm và số lượng cho vật thể mới
            ItemPickup pickupScript = droppedObj.GetComponent<ItemPickup>();
            if (pickupScript != null)
            {
                pickupScript.itemData = item;
                pickupScript.amount = amount;
            }

            Debug.Log("Đã vứt " + amount + " cái " + item.itemName + " ra đất!");
        }
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];
        
        if(slot.itemData == null)
            return;
        
        slot.itemData.Use();
    }

    void Update()
    {
        // Bấm nút K trên bàn phím để vứt bớt 3 món đồ test
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (slots.Count > 0)
            {
                ItemData itemCanVut = slots[0].itemData;

                bool daVut = RemoveItem(itemCanVut, 3);

                if (daVut)
                {
                    Debug.Log("Đã vứt 3 cái " + itemCanVut.itemName);
                    // ĐÃ LOẠI BỎ ĐOẠN CODE CODE FIND_OBJECT_OF_TYPE CŨ VÌ EVENT TỰ ĐỘNG XỬ LÝ
                }
                else
                {
                    Debug.Log("Không đủ đồ để vứt!");
                }
            }
        }

        // Bấm nút J trên bàn phím để thả đồ ra đất
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (slots.Count > 0)
            {
                ItemData itemMuonDrop = slots[0].itemData;

                // Thả 2 vật phẩm đầu tiên trong túi ra đất
                DropItem(itemMuonDrop, 1);
                // ĐÃ LOẠI BỎ ĐOẠN CODE CODE FIND_OBJECT_OF_TYPE CŨ VÌ EVENT TỰ ĐỘNG XỬ LÝ
            }
        }
    }
}