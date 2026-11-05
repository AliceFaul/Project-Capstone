// RewardSlotUI.cs - gắn vào prefab 1 ô phần thưởng
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image glowBorder; // viền phát sáng theo độ hiếm
    [SerializeField] private TMP_Text nameText;

    public void Setup(WeaponData w)
    {
        icon.sprite = w.icon;
        nameText.text = w.weaponName;
        glowBorder.color = RarityColors.Get(w.rarity);
    }
}