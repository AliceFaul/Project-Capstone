using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentProgressConfig", menuName = "Config/Equipment Progression")]
public class EquipmentProgressConfig : ScriptableObject, IConfig
{
    [Header("Level")]
    public int maxLevel = 10;
    [Tooltip("X = level hien tai (1..maxLevel-1), Y = EXP can de len level ke tiep.")]
    public AnimationCurve expToNextLevel = AnimationCurve.Linear(1f, 100f, 9f, 900f);
 
    [Header("He so theo level (X = level). Level chi tang nhe damage, khong tang nhieu.")]
    public AnimationCurve damageMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 1.2f);
    [Tooltip("Mac dinh phang (1.0) = giap len level khong tang phong thu.")]
    public AnimationCurve defenseMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 1f);
 
    [Header("Socket (chi vu khi)")]
    public int[] socketUnlockLevels = { 3, 5, 10 };
    [Tooltip("Phi go rune = level vu khi x gia tri nay (Gem).")]
    public int gemPerLevelToRemoveRune = 100;
 
    [Header("Roll tier (trong so, khong can cong dung 100)")]
    public float[] tierWeights = { 25f, 25f, 25f, 25f };
    
    [Header("Tra cuu Rune")]
    [Tooltip("Keo CUNG asset ItemDatabase da gan trong PlayerDataConfig. StatResolver can de doc RuneData theo id.")]
    [SerializeField] private ItemDatabase itemDatabase;
    [System.NonSerialized] private bool _warnedMissingDatabase;
 
    public float GetExpToNextLevel(int level) => Mathf.Max(1f, expToNextLevel.Evaluate(level));
 
    public float GetLevelMultiplier(BonusStat stat, int level)
    {
        switch (stat)
        {
            case BonusStat.Damage: return damageMultiplier.Evaluate(level);
            case BonusStat.Defense: return defenseMultiplier.Evaluate(level);
            default: return 1f;
        }
    }

    public RuneData GetRune(string runeId)
    {
        if (itemDatabase == null)
        {
            if (!_warnedMissingDatabase)
            {
                Debug.LogError($"[EquipmentProgressConfig] Chua gan ItemDatabase - rune se KHONG co tac dung.");
                _warnedMissingDatabase = true;
            }
            return null;
        }

        return itemDatabase.Get(runeId) as RuneData;
    }
 
    public bool IsSocketUnlocked(int level, int socketIndex)
    {
        return socketUnlockLevels != null
               && socketIndex >= 0
               && socketIndex < socketUnlockLevels.Length
               && level >= socketUnlockLevels[socketIndex];
    }
 
    public int GetRuneRemovalCost(int level) => level * gemPerLevelToRemoveRune;
}
 
public static class EquipmentLeveling
{
    // Cong EXP, tu dong len cap neu du. Tra ve so cap tang them.
    public static int AddExp(EquipmentInstance instance, float amount, EquipmentProgressConfig config)
    {
        if (instance == null || config == null || amount <= 0f) return 0;
        if (instance.level >= config.maxLevel) return 0;
 
        instance.exp += amount;
        int gained = 0;
 
        while (instance.level < config.maxLevel)
        {
            float need = config.GetExpToNextLevel(instance.level);
            if (instance.exp < need) break;
            instance.exp -= need;
            instance.level++;
            gained++;
        }
 
        if (instance.level >= config.maxLevel) instance.exp = 0f;
        return gained;
    }
}