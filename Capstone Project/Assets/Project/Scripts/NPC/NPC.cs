using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "Trưởng Làng";
    public QuestData questData;
    public bool hasGivenQuest = false; // Biến kiểm tra NPC đã giao Quest chưa

    [Header("Dialogue")]
    [TextArea(3, 5)]
    public string[] sentences;

    private void OnMouseDown()
    {
        if (DialogueManager.Instance == null) return;
        if (DialogueManager.Instance.IsDialogueActive) return;

        // Nếu đã nhận quest rồi thì không cho bật lại bảng nhận quest nữa
        if (hasGivenQuest)
        {
            Debug.Log($"{npcName} đang đứng yên, không có nhiệm vụ mới.");
            return;
        }

        // Truyền thêm (this) để DialogueManager biết chính xác NPC nào đang nói chuyện
        DialogueManager.Instance.StartDialogue(npcName, sentences, questData, this);
    }

    // Hàm gọi từ DialogueManager khi người chơi bấm [Chấp nhận]
    public void SetIdleState()
    {
        hasGivenQuest = true;
        // Nếu ông có Animator đổi animation đứng yên thì bật ở đây:
        // GetComponent<Animator>()?.SetTrigger("Idle");
    }
}