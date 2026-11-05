using UnityEngine;

public class ChestNPC : MonoBehaviour
{
    [SerializeField] private GameObject gachaPanel;

    // Gọi từ script click / raycast NPC (cùng kiểu với NPCInteractable)
    public void OnPlayerInteract()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        gachaPanel.SetActive(true);
    }
}