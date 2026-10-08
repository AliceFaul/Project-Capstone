using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    public static QuestLogUI Instance { get; private set; }

    [Header("UI Panel")]
    [SerializeField] private GameObject questLogPanel;

    [Header("Danh sách & Prefab")]
    [SerializeField] private Transform questListContent;
    [SerializeField] private GameObject questItemPrefab;

    [Header("Khung Chi Tiết")]
    [SerializeField] private TextMeshProUGUI detailTitleText;
    [SerializeField] private TextMeshProUGUI detailDescText;

    private QuestType currentFilter = QuestType.LobbyDaily;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (questLogPanel != null)
            questLogPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleQuestLog();
        }
    }

    public void ToggleQuestLog()
    {
        if (questLogPanel == null) return;

        bool isActive = !questLogPanel.activeSelf;
        questLogPanel.SetActive(isActive);

        if (isActive)
        {
            FilterByLobbyDaily();
        }
    }

    public void FilterByLobbyDaily() => FilterQuests(QuestType.LobbyDaily);
    public void FilterByInRun() => FilterQuests(QuestType.InRun);

    public void FilterQuests(QuestType type)
    {
        currentFilter = type;
        RefreshQuestList();
    }

    public void RefreshQuestList()
    {
        if (questListContent == null) return;

        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        ClearDetails();

        if (QuestManager.Instance == null) return;

        // Lấy danh sách nhiệm vụ từ QuestManager dựa trên filter
        List<ActiveQuest> targetList = (currentFilter == QuestType.LobbyDaily)
            ? QuestManager.Instance.activeLobbyQuests
            : QuestManager.Instance.activeInRunQuests;

        if (targetList == null || targetList.Count == 0) return;

        // Tạo ra các nút nhiệm vụ trong danh sách
        foreach (ActiveQuest quest in targetList)
        {
            if (questItemPrefab == null) break;

            GameObject itemObj = Instantiate(questItemPrefab, questListContent);
            QuestItemUI itemScript = itemObj.GetComponent<QuestItemUI>();

            if (itemScript != null)
            {
                // Truyền ActiveQuest và gửi hàm ShowQuestDetails làm callback khi click
                itemScript.Setup(quest, ShowQuestDetails);
            }
        }

        // Hiển thị chi tiết nv đầu tiên
        if (targetList.Count > 0)
        {
            ShowQuestDetails(targetList[0]);
        }
    }

    public void ShowQuestDetails(ActiveQuest quest)
    {
        if (quest == null || quest.data == null) return;

        if (detailTitleText != null)
            detailTitleText.text = quest.data.questTitle;

        if (detailDescText != null)
            detailDescText.text = $"{quest.data.questDescription}\n\nTiến độ: {quest.currentAmount}/{quest.data.requiredAmount}";
    }

    private void ClearDetails()
    {
        if (detailTitleText != null) detailTitleText.text = "Chưa chọn nhiệm vụ";
        if (detailDescText != null) detailDescText.text = "Không có nhiệm vụ nào trong mục này.";
    }
}