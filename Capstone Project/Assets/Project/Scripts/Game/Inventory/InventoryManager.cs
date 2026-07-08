using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Cấu hình Giao diện")]
    [SerializeField] private GameObject inventoryPanel; // Kéo Inventory_Panel vào đây
    [SerializeField] private Transform gridSlots;       // Kéo Grid_Slots vào đây

    [Header("Kết nối Dữ liệu")]
    [SerializeField] private PlayerInventory playerInventory; // Kéo Player vào đây

    private Image[] slotIcons;
    private TextMeshProUGUI[] slotTexts;

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

    private void InitUI()
    {
        int totalSlots = gridSlots.childCount;
        slotIcons = new Image[totalSlots];
        slotTexts = new TextMeshProUGUI[totalSlots];

        for (int i = 0; i < totalSlots; i++)
        {
            Transform slotTransform = gridSlots.GetChild(i);
            // Tìm đúng tên 2 object con nằm trong ô UI_Slot
            slotIcons[i] = slotTransform.Find("Item_Icon").GetComponent<Image>();
            slotTexts[i] = slotTransform.Find("Stack_Text").GetComponent<TextMeshProUGUI>();
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
        for (int i = 0; i < slotIcons.Length; i++)
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