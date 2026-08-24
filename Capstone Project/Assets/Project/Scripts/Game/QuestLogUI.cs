using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestLogUI : MonoBehaviour
{
    public static QuestLogUI Instance { get; private set; }

    [Header("UI Panel")]
    [SerializeField] private GameObject questLogPanel;

    [Header("Danh sách & Prefab")]
    [SerializeField] private Transform questListContent; // Khung chứa danh sách các nút Quest
    [SerializeField] private GameObject questItemPrefab; // Prefab của QuestItemUI

    [Header("Khung Chi Tiết")]
    [SerializeField] private TextMeshProUGUI detailTitleText;
    [SerializeField] private TextMeshProUGUI detailDescText;

    private QuestType currentFilter = QuestType.LobbyDaily; // Đổi mặc định sang LobbyDaily

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Ẩn panel lúc bắt đầu game
        if (questLogPanel != null)
            questLogPanel.SetActive(false);
    }

    private void Update()
    {
        // Kiểm tra nhấn phím J
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
            FilterByLobbyDaily(); // Mặc định hiện Quest LobbyDaily khi mở
        }
    }

    // Các hàm gán vào nút Tab phân loại trên UI
    public void FilterByLobbyDaily() => FilterQuests(QuestType.LobbyDaily);
    public void FilterByInRun() => FilterQuests(QuestType.InRun);

    public void FilterQuests(QuestType type)
    {
        currentFilter = type;
        RefreshQuestList();
    }

    public void RefreshQuestList()
    {
        // 1. Kiểm tra an toàn cho questListContent để tránh crash
        if (questListContent == null) return;

        // Xóa danh sách nút cũ
        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        ClearDetails();

        // 2. Kiểm tra an toàn cho PlayerQuestManager và acceptedQuests
        if (PlayerQuestManager.Instance == null || PlayerQuestManager.Instance.acceptedQuests == null)
        {
            return;
        }

        // Lấy toàn bộ quest từ PlayerQuestManager và lọc theo tab
        List<QuestData> allQuests = PlayerQuestManager.Instance.acceptedQuests;
        List<QuestData> filteredList = allQuests.FindAll(q => q != null && q.questType == currentFilter);

        // Tạo ra các nút nhiệm vụ trong danh sách
        foreach (QuestData quest in filteredList)
        {
            if (questItemPrefab == null) break;

            GameObject itemObj = Instantiate(questItemPrefab, questListContent);
            QuestItemUI itemScript = itemObj.GetComponent<QuestItemUI>();
            if (itemScript != null)
            {
                itemScript.Setup(quest, this);
            }
        }

        // Hiển thị chi tiết nv đầu tiên nếu tìm thấy
        if (filteredList.Count > 0)
        {
            ShowQuestDetails(filteredList[0]);
        }
    }

    public void ShowQuestDetails(QuestData quest)
    {
        if (quest == null) return;
        if (detailTitleText != null) detailTitleText.text = quest.questTitle;
        if (detailDescText != null) detailDescText.text = quest.questDescription;
    }

    private void ClearDetails()
    {
        if (detailTitleText != null) detailTitleText.text = "Chưa chọn nhiệm vụ";
        if (detailDescText != null) detailDescText.text = "Không có nhiệm vụ nào trong mục này.";
    }
}