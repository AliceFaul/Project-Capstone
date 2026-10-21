using UnityEngine;

public class NPCChestInteraction : MonoBehaviour
{
    [Header("UI / Gacha Reference")]
    [SerializeField] private GameObject gachaUIPanel; // Panel chứa nút Mở x1 / Mở x10 sau này

    private void OnMouseDown()
    {
        // Khi người chơi click chuột trái vào NPC này
        Debug.Log("[Lobby] Đã tương tác với NPC Gacha!");

        if (gachaUIPanel != null)
        {
            bool isActive = gachaUIPanel.activeSelf;
            gachaUIPanel.SetActive(!isActive); // Bật/tắt giao diện quay rương
        }
        else
        {
            Debug.LogWarning("[Lobby] Chưa gán Panel Gacha UI vào NPC!");
        }
    }
}