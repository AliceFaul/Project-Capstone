using UnityEngine;

public class PlayerQuestManager : MonoBehaviour
{
    public static PlayerQuestManager Instance { get; private set; }

    [SerializeField] private QuestData activeQuest;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AcceptQuest(QuestData quest)
    {
        activeQuest = quest;
        Debug.Log($"<color=green>[QUEST ACCEPTED]</color> Đã nhận nhiệm vụ: {quest.questTitle}");
    }
}