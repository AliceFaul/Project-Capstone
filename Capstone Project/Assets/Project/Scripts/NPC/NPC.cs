using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("Thông tin NPC")]
    public string npcName = "Trưởng Làng";

    [Header("Lời thoại NPC")]
    [TextArea(3, 5)]
    public string[] sentences;

    private void OnMouseDown()
    {
        // Nếu thoại đang hiện rồi thì click vào NPC sẽ không kích hoạt lại từ đầu
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        // Gọi DialogueManager để bắt đầu hội thoại
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(npcName, sentences);
        }
    }
}