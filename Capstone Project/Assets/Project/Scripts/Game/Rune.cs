using UnityEngine;

public enum RuneType { Attack, Crit, Speed, Defense, StatusEffect }
public enum StatusEffectType { None, Burn, Poison, Freeze, Paralyze }

[System.Serializable]
public class RuneModifier
{
    public float damageModifier;
    public float critChanceModifier;
    public float critDamageModifier;
    public float attackSpeedModifier;
    public float moveSpeedModifier;
}

/// Vật phẩm Rune: khảm vào ô vũ khí để nhận chỉ số
/// hiệu ứng trạng thái (đốt, tê liệt, độc) khi đánh trúng
[CreateAssetMenu(menuName = "Inventory/Rune Data", fileName = "New Rune Data")]
public class RuneData : ItemData
{
    [Header("Rune")]
    public RuneType runeType;

    [Header("Stat Modify")]
    [Tooltip("Dùng khi runeType là Attack / Crit / Speed / Defense.")]
    public RuneModifier modifier;

    [Header("Status Effect")]
    [Tooltip("Dùng khi runeType = StatusEffect.")]
    public StatusEffectType statusEffect;
    public float statusEffectValue;    // VD: cho sát thương đốt/độc mỗi stack
    public float statusEffectDuration; // thời gian hiệu ứng tồn tại

    [Range(0f, 1f)]
    [Tooltip("Tỉ lệ % để hiệu ứng trạng thái trigger mỗi lần đánh trúng.")]
    public float triggerChance = 1f;
}