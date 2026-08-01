using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Cấu hình vật phẩm rơi")]
    public ItemData itemData; // Chuyển sang hẳn public để PlayerInventory.cs truy cập được khi vứt đồ
    public int amount = 1;    // Số lượng rơi trên đất

    // Thay bằng OnTriggerStay để tối ưu va chạm cho NavMeshAgent, tránh bị hụt khi di chuyển quá nhanh
    private void OnTriggerStay(Collider other)
    {
        // Tìm component PlayerInventory trên vật thể va chạm hoặc cha của nó
        PlayerInventory playerInventory = other.GetComponentInParent<PlayerInventory>();

        // Nếu tìm thấy túi đồ (nghĩa là Player có va chạm)
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