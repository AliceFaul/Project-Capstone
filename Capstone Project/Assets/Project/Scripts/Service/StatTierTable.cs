using UnityEngine;

[System.Serializable]
public class StatTierTable
{
    public const int TierCount = 4;
 
    public BonusStat Stat;
 
    [Tooltip("Bat: chi so co 4 tier, roll ngau nhien khi nhat/mua. Tat: gia tri co dinh (chi dung phan tu dau).")]
    public bool Tiered = true;
 
    [Tooltip("Gia tri tier 1..4 (tier 1 thap nhat). Vi du kiem go damage: 5, 7, 10, 12.")]
    public float[] TierValues = new float[TierCount];
 
    public float GetValue(int tier)
    {
        if (TierValues == null || TierValues.Length == 0) return 0f;
        if (!Tiered) return TierValues[0];
        return TierValues[Mathf.Clamp(tier, 0, TierValues.Length - 1)];
    }
}