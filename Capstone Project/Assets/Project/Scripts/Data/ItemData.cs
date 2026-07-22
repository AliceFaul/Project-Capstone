using UnityEngine;

// Tạo đường dẫn trong menu chuột phải của Unity để tạo file data mới
[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string id;               // ID dùng để quản lý mã vật phẩm
    public string itemName;         // Tên hiển thị của vật phẩm trong game
    public Sprite icon;             // Ảnh đại diện của vật phẩm trên UI (NẾU CÓ)
    public bool isStackable = true; // Bật tắt tính năng cộng dồn vật phẩm
    public int maxStackSize = 64;   // Số lượng tối đa cho phép trong một ô
}