using System.Collections.Generic; // Giữ thêm để dùng List
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Cấu hình Giao diện")]
    [SerializeField] private GameObject inventoryPanel; // Kéo thả Inventory_Panel 
    [SerializeField] private Transform gridSlots;       // Kéo thả Grid_Slots 

    [Header("Cấu hình Tự động tạo Slot (Nâng cấp)")]
    [SerializeField] private GameObject slotPrefab;     // Kéo file mẫu UI_Slot (Prefab) vào đây ngoài Inspector

    [Header("Kết nối Dữ liệu")]
    [SerializeField] private PlayerInventory playerInventory; // Kéo Player vào

    // Đổi từ Array sang List để tự động thêm phần tử khi sinh slot bằng code
    private List<Image> slotIcons = new List<Image>();
    private List<TextMeshProUGUI> slotTexts = new List<TextMeshProUGUI>();

    //ĐĂNG KÝ EVENT ĐỂ TỰ ĐỘNG CẬP NHẬT SLOT
    private void OnEnable()
    {
        PlayerInventory.OnInventoryChanged += UpdateInventoryUI;
    }

    private void OnDisable()
    {
        PlayerInventory.OnInventoryChanged -= UpdateInventoryUI;
    }
    // --------------------------------------------------

    private void Start()
    {
        InitUI();
        if (inventoryPanel != null) inventoryPanel.SetActive(false); // Mặc định ẩn khi vào game
    }

    private void Update()
    {
        // Nhấn B để đóng/mở kho đồ
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleInventory();
        }
    }

    // nâng cấp logic cho hàm: Bấm play sẽ tự tạo 15 slot (initui)
    private void InitUI()
    {
        // Xóa sạch các ô cũ lỡ tay để lại ngoài Hierarchy để tránh trùng lặp
        foreach (Transform child in gridSlots)
        {
            Destroy(child.gameObject);
        }

        slotIcons.Clear();
        slotTexts.Clear();

        // Vòng lặp tự động tạo đúng 15 ô đồ từ file mẫu Prefab
        for (int i = 0; i < 15; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, gridSlots);
            newSlot.name = "UI_Slot_" + i;

            Transform slotTransform = newSlot.transform;
            // Tìm đúng tên 2 object con nằm trong ô UI_Slot (Giữ nguyên logic cũ nhưng nạp vào List)
            slotIcons.Add(slotTransform.Find("Item_Icon").GetComponent<Image>());
            slotTexts.Add(slotTransform.Find("Stack_Text").GetComponent<TextMeshProUGUI>());
        }
    }

    private void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isActive);

            if (isActive)
            {
                UpdateInventoryUI();
            }
        }
    }

    // Hàm đồng bộ dữ liệu ngầm lên giao diện 15 ô vuông
    public void UpdateInventoryUI()
    {
        // Nếu giao diện đang đóng thì không cần chạy vòng lặp cập nhật cho đỡ tốn hiệu năng
        if (inventoryPanel != null && !inventoryPanel.activeSelf) return;

        for (int i = 0; i < slotIcons.Count; i++)
        {
            if (i < playerInventory.slots.Count)
            {
                // Nếu ô đó có đồ -> Hiển thị Icon và Số lượng
                slotIcons[i].sprite = playerInventory.slots[i].itemData.icon;
                slotIcons[i].enabled = true;

                if (playerInventory.slots[i].stackSize > 1)
                {
                    slotTexts[i].text = playerInventory.slots[i].stackSize.ToString();
                    slotTexts[i].enabled = true;
                }
                else
                {
                    slotTexts[i].enabled = false; // Bằng 1 thì ẩn số đi cho đẹp
                }
            }
            else
            {
                // Nếu ô đó trống -> Ẩn icon và chữ số
                slotIcons[i].enabled = false;
                slotTexts[i].enabled = false;
            }
        }
    }
}