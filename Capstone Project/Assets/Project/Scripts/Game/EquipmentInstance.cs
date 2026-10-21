using UnityEngine;

[System.Serializable]
public class EquipmentInstance
{
    public ItemData equipmentData;
    public EquipmentType type; // Phân loại: vũ khí hoặc giáp

    [Header("Level & EXP (Tối đa Cấp 10)")]
    public int currentLevel = 1;
    public const int MAX_LEVEL = 10;

    public int currentEXP = 0;
    public int totalAccumulatedEXP = 0; //Tích lũy toàn bộ kinh nghiệm để tính 50% khi Feed

    public int GetRequiredEXP() => currentLevel * 100;

    /// Thêm kinh nghiệm chung cho cả Vũ khí và Giáp
    public void AddEXP(int expGained)
    {
        if (currentLevel >= MAX_LEVEL) return;

        currentEXP += expGained;
        totalAccumulatedEXP += expGained;

        while (currentEXP >= GetRequiredEXP() && currentLevel < MAX_LEVEL)
        {
            currentEXP -= GetRequiredEXP();
            currentLevel++;
            Debug.Log($"[{type} Level Up] {equipmentData.itemName} đã đạt Cấp {currentLevel}!");
        }

        if (currentLevel >= MAX_LEVEL) currentEXP = 0;
    }
}