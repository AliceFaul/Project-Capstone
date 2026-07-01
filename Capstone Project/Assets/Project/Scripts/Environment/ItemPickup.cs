using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Cấu hình vật phẩm rơi")]
    [SerializeField] private ItemData itemData; // Kéo file dữ liệu vật phẩm vào chỗ này
    [SerializeField] private int amount = 1;    // Số lượng rơi trên đất

    // Hàm tự động kích hoạt khi có vật thể khác đi xuyên qua Collider (Is Trigger)
    private void OnTriggerEnter(Collider other)
    {
        // Tìm component PlayerInventory trên vật thể va chạm hoặc cha của nó
        PlayerInventory playerInventory = other.GetComponentInParent<PlayerInventory>();

        // Nếu tìm thấy túi đồ (nghĩa là Player va chạm)
        if (playerInventory != null)
        {
            // Tiến hành thêm vật phẩm vào kho đồ
            bool pickedUpSuccessfully = playerInventory.AddItem(itemData, amount);

            // Nếu thêm thành công (túi còn chỗ)
            if (pickedUpSuccessfully)
            {
                Debug.Log($"[Inventory] Đã nhặt: {amount}x {itemData.itemName}");
                Destroy(gameObject); // Xóa vật phẩm khỏi map
            }
        }
    }
}