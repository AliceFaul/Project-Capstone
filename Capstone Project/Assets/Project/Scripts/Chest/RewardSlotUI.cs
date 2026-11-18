using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    public void Setup(EquipmentData w)
    {
        if (w == null) return;

        if (nameText != null)
        {
            nameText.text = w.itemName;
        }

        if (iconImage != null && w.icon != null)
        {
            iconImage.sprite = w.icon;
            iconImage.gameObject.SetActive(true);
        }
    }
}