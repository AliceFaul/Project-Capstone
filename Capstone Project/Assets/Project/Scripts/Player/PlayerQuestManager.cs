using System.Collections.Generic;
using UnityEngine;

public class PlayerQuestManager : MonoBehaviour
{
    public static PlayerQuestManager Instance { get; private set; }

    // Danh sách nhiệm vụ đã nhận
    public List<QuestData> acceptedQuests = new List<QuestData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Hàm nhận quest chuẩn chỉnh
    public void AcceptQuest(QuestData quest)
    {
        if (quest != null && !acceptedQuests.Contains(quest))
        {
            acceptedQuests.Add(quest);
            Debug.Log("Đã thêm vào acceptedQuests: " + quest.questTitle);
        }
    }
}