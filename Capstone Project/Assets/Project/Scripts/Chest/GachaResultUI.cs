// GachaResultUI.cs
using System.Collections.Generic;
using UnityEngine;

public class GachaResultUI : MonoBehaviour
{
    [Header("Kết quả x1")]
    [SerializeField] private GameObject singlePanel;
    [SerializeField] private RewardSlotUI singleSlot;

    [Header("Tổng kết x10")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private Transform summaryGrid; // GridLayoutGroup: Fixed Column Count = 5
    [SerializeField] private RewardSlotUI slotPrefab;

    private System.Action onClose;

    public void ShowSingle(WeaponData w, System.Action onCloseCallback)
    {
        onClose = onCloseCallback;
        singleSlot.Setup(w);
        singlePanel.SetActive(true);
    }

    public void ShowSummary(List<WeaponData> weapons, System.Action onCloseCallback)
    {
        onClose = onCloseCallback;
        foreach (Transform child in summaryGrid) Destroy(child.gameObject);
        foreach (var w in weapons) Instantiate(slotPrefab, summaryGrid).Setup(w);
        summaryPanel.SetActive(true); // 10 ô, 2 hàng x 5
    }

    // Gắn vào nút Đóng / Xác nhận của cả 2 panel (OnClick)
    public void Close()
    {
        singlePanel.SetActive(false);
        summaryPanel.SetActive(false);
        onClose?.Invoke();
        onClose = null;
    }
}