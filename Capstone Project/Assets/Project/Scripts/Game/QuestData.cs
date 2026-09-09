using UnityEngine;

// Phân loại nv Quest Log
public enum QuestType
{
    Lobby,      // Nhiệm vụ sảnh
    LobbyDaily, // Nhiệm vụ mỗi ngày
    InRun,      // Nhiệm vụ nhận từ NPC trong màn chơi
    Main,       // Nhiệm vụ chính
    Side        // Nhiệm vụ phụ
}

public enum QuestGoalType
{
    CollectItem, // Nhặt vật phẩm (Gỗ, Đá, Thuốc)
    KillEnemy    // Tiêu diệt quái vật
}

[CreateAssetMenu(fileName = "NewQuestData", menuName = "Quest System/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("1. THÔNG TIN BẢN CHẤT")]
    public string questID;
    public string questTitle;          // Tên nhiệm vụ
    [TextArea(3, 5)]
    public string questDescription;    // Mô tả nhiệm vụ
    public QuestType questType;        // Loại nhiệm vụ (Lobby, Main, Side)

    [Header("2. ĐIỀU KIỆN HOÀN THÀNH")]
    public QuestGoalType goalType;     // Loại mục tiêu (Nhặt đồ hay Diệt quái)
    public ItemData targetItem;        // Món đồ cần thu thập
    public string targetEnemyID;       // ID quái cần diệt
    public int requiredAmount = 1;     // Số lượng cần đạt

    [Header("3. PHẦN THƯỞNG")]
    public int rewardGold;              // Số vàng nhận được
    public ItemData rewardItem;        // Vật phẩm thưởng
    public int rewardItemAmount = 1;   // Số lượng vật phẩm thưởng
}