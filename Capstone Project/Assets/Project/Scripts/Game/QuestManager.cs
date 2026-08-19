using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Bổ sung thư viện quản lý Scene

[System.Serializable]
public class ActiveQuest
{
    public QuestData data;
    public int currentAmount;
    public bool isCompleted;

    public ActiveQuest(QuestData questData)
    {
        data = questData;
        currentAmount = 0;
        isCompleted = false;
    }
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Danh sách Nhiệm vụ đang nhận")]
    public List<ActiveQuest> activeLobbyQuests = new List<ActiveQuest>();
    public List<ActiveQuest> activeInRunQuests = new List<ActiveQuest>();

    [Header("Cấu hình Daily Quest (4 Tiếng Reset)")]
    public List<QuestData> dailyQuestPool; // Bể chứa các Quest Daily để chọn ngẫu nhiên
    public int maxDailyQuests = 3;         // Số lượng Quest Daily nhận mỗi đợt
    private const string LAST_RESET_TIME_KEY = "LastDailyQuestResetTime";
    private readonly TimeSpan resetInterval = TimeSpan.FromHours(4); // Reset sau 4 tiếng

    [Header("Cấu hình Scene")]
    [SerializeField] private string lobbySceneName = "LobbyScene"; // Đổi tên thành tên Scene Lobby trong project

    public static event Action OnQuestUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CheckAndResetDailyQuests();
    }

    private void OnEnable()
    {
        PlayerInventory.OnInventoryChanged += CheckCollectItemQuests;
        SceneManager.sceneLoaded += OnSceneLoaded; // Lắng nghe event chuyển Scene
    }

    private void OnDisable()
    {
        PlayerInventory.OnInventoryChanged -= CheckCollectItemQuests;
        SceneManager.sceneLoaded -= OnSceneLoaded; // Hủy lắng nghe event chuyển Scene
    }

    // Tự động kiểm tra mỗi khi chuyển sang Scene mới
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == lobbySceneName)
        {
            ClearInRunQuests();
        }
    }

    #region --- LOGIC IN-RUN QUEST ---
    /// <summary>
    /// Xóa toàn bộ nhiệm vụ ngắn hạn khi kết thúc Run / quay về Lobby
    /// </summary>
    public void ClearInRunQuests()
    {
        if (activeInRunQuests.Count > 0)
        {
            activeInRunQuests.Clear();
            Debug.Log("[QuestManager] Đã tự động dọn sạch các In-Run Quests khi về Lobby!");
            OnQuestUpdated?.Invoke();
        }
    }
    #endregion

    #region THÊM NHIỆM VỤ
    public bool AddQuest(QuestData quest)
    {
        if (quest == null) return false;

        ActiveQuest newQuest = new ActiveQuest(quest);

        if (quest.questType == QuestType.LobbyDaily)
        {
            activeLobbyQuests.Add(newQuest);
        }
        else
        {
            activeInRunQuests.Add(newQuest);
        }

        Debug.Log($"[QuestManager] Đã nhận nhiệm vụ: {quest.questTitle}");

        if (quest.goalType == QuestGoalType.CollectItem)
        {
            CheckCollectItemQuests();
        }

        OnQuestUpdated?.Invoke();
        return true;
    }
    #endregion

    #region XỬ LÝ TIẾN ĐỘ: DIỆT QUÁI
    public void OnEnemyKilled(string enemyID)
    {
        UpdateKillEnemyProgress(activeLobbyQuests, enemyID);
        UpdateKillEnemyProgress(activeInRunQuests, enemyID);

        OnQuestUpdated?.Invoke();
    }

    private void UpdateKillEnemyProgress(List<ActiveQuest> questList, string enemyID)
    {
        foreach (var quest in questList)
        {
            if (quest.isCompleted || quest.data.goalType != QuestGoalType.KillEnemy) continue;

            if (quest.data.targetEnemyID == enemyID)
            {
                quest.currentAmount++;
                Debug.Log($"[QuestManager] Đã diệt {enemyID}: {quest.currentAmount}/{quest.data.requiredAmount}");

                if (quest.currentAmount >= quest.data.requiredAmount)
                {
                    CompleteQuest(quest);
                }
            }
        }
    }
    #endregion

    #region XỬ LÝ TIẾN ĐỘ: NHẶT ĐỒ
    private void CheckCollectItemQuests()
    {
        PlayerInventory playerInv = FindAnyObjectByType<PlayerInventory>();
        if (playerInv == null) return;

        UpdateCollectItemProgress(activeLobbyQuests, playerInv);
        UpdateCollectItemProgress(activeInRunQuests, playerInv);

        OnQuestUpdated?.Invoke();
    }

    private void UpdateCollectItemProgress(List<ActiveQuest> questList, PlayerInventory inv)
    {
        foreach (var quest in questList)
        {
            if (quest.isCompleted || quest.data.goalType != QuestGoalType.CollectItem) continue;

            int totalCount = 0;
            foreach (var slot in inv.slots)
            {
                if (slot.itemData == quest.data.targetItem)
                {
                    totalCount += slot.stackSize;
                }
            }

            quest.currentAmount = totalCount;

            if (quest.currentAmount >= quest.data.requiredAmount)
            {
                CompleteQuest(quest);
            }
        }
    }
    #endregion

    #region LOGIC RESET DAILY QUEST (SYSTEM.DATETIME & PLAYERPREFS)
    public void CheckAndResetDailyQuests()
    {
        string lastResetString = PlayerPrefs.GetString(LAST_RESET_TIME_KEY, string.Empty);
        DateTime now = DateTime.Now;

        if (string.IsNullOrEmpty(lastResetString))
        {
            GenerateNewDailyQuests();
            SaveResetTime(now);
        }
        else
        {
            if (DateTime.TryParse(lastResetString, out DateTime lastResetTime))
            {
                TimeSpan elapsed = now - lastResetTime;

                if (elapsed >= resetInterval)
                {
                    GenerateNewDailyQuests();
                    SaveResetTime(now);
                }
                else
                {
                    Debug.Log($"[QuestManager] Daily Quest chưa reset. Thời gian còn lại: {GetRemainingTimeFormat()}");
                }
            }
            else
            {
                GenerateNewDailyQuests();
                SaveResetTime(now);
            }
        }
    }

    private void GenerateNewDailyQuests() // hàm tạo danh sách nv hằng ngày mới
    {
        activeLobbyQuests.Clear();

        if (dailyQuestPool == null || dailyQuestPool.Count == 0) return;

        // Xáo trộn danh sách (Fisher-Yates Shuffle)
        List<QuestData> shuffledPool = new List<QuestData>(dailyQuestPool);
        for (int i = 0; i < shuffledPool.Count; i++)
        {
            QuestData temp = shuffledPool[i];
            int randomIndex = UnityEngine.Random.Range(i, shuffledPool.Count);
            shuffledPool[i] = shuffledPool[randomIndex];
            shuffledPool[randomIndex] = temp;
        }

        int countToPick = Mathf.Min(maxDailyQuests, shuffledPool.Count);
        for (int i = 0; i < countToPick; i++)
        {
            AddQuest(shuffledPool[i]);
        }

        Debug.Log($"[QuestManager] Đã làm mới {countToPick} Daily Quest!");
    }

    private void SaveResetTime(DateTime time) // lấy chính xác mốc 4 giờ, cho dù có tắt game hoặc đổi múi giờ   
    {
        PlayerPrefs.SetString(LAST_RESET_TIME_KEY, time.ToString("o")); // Chuỗi ISO 8601 chuẩn
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Trả về chuỗi thời gian còn lại (HH:MM:SS) để gắn lên UI đồng hồ
    /// </summary>
    public string GetRemainingTimeFormat()
    {
        string lastResetString = PlayerPrefs.GetString(LAST_RESET_TIME_KEY, string.Empty);
        if (DateTime.TryParse(lastResetString, out DateTime lastResetTime))
        {
            DateTime nextResetTime = lastResetTime + resetInterval;
            TimeSpan timeRemaining = nextResetTime - DateTime.Now;

            if (timeRemaining <= TimeSpan.Zero)
            {
                return "00:00:00";
            }

            return string.Format("{0:D2}:{1:D2}:{2:D2}", timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
        }

        return "00:00:00";
    }
    #endregion

    #region HOÀN THÀNH NHIỆM VỤ
    private void CompleteQuest(ActiveQuest quest)
    {
        quest.isCompleted = true;
        Debug.Log($"[HOÀN THÀNH NHIỆM VỤ]: {quest.data.questTitle}!");
    }
    #endregion
}