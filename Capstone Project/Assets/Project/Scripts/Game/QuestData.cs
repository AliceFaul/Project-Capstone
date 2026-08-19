using UnityEngine;

// 1. Phân loại nv theo yêu cầu (Lobby vs In-Run từ NPC)
public enum QuestType
{
    LobbyDaily, // Nhiệm vụ mỗi ngày nhận ở Lobby (Reset mỗi 4 tiếng)
    InRun       // Nhiệm vụ nhận từ NPC trong màn chơi để lấy vật phẩm
}

// 2. Loại mục tiêu cần hoàn thành
public enum QuestGoalType
{
    CollectItem, // Nhặt vật phẩm (Gỗ, Đá, Thuốc,...)
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
    public QuestType questType;        // LobbyDaily hay InRun

    [Header("2. ĐIỀU KIỆN HOÀN THÀNH")]
    public QuestGoalType goalType;     // Loại mục tiêu (Nhặt đồ hay Diệt quái)
    public ItemData targetItem;        // Món đồ cần thu thập
    public string targetEnemyID;       // ID quái cần diệt
    public int requiredAmount = 1;     // Số lượng cần đạt

    [Header("3. PHẦN THƯỞNG")]
    public int rewardGold;             // Số tiền/vàng nhận được
    public ItemData rewardItem;        // Vật phẩm thưởng
    public int rewardItemAmount = 1;   // Số lượng vật phẩm thưởng
}