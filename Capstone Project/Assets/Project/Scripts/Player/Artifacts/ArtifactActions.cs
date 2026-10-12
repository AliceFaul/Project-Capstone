using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// DANH SACH cac Action cua Artifact. Muon them hanh vi moi: tao class moi
// ke thua ArtifactAction, danh dau [Serializable] - 
// no tu hien trong dropdown cua ArtifactData.actions (SerializeReferenceDropdown).

[Serializable]
public class StatBuffAction : ArtifactAction
{
    [Tooltip("De trong = dung id cua artifact")]
    public string buffId;
    public float duration = 8f;
    public List<StatBuffEntry> entries = new();

    public override IEnumerable<BonusStat> BuffStats
    {
        get
        {
            foreach (var entry in entries) yield return entry.stat;
        }
    }

    public override void Execute(ArtifactContext context)
    {
        if (context.Buffs == null)
        {
            Debug.LogError($"[StatBuffAction] Player missing PlayerBuffs component.");
            return;
        }
        
        var modifiers = new List<StatModifier>();
        foreach(var entry in entries) modifiers.Add(new StatModifier(entry.stat, entry.value, entry.mode));
        context.Buffs.Apply(string.IsNullOrEmpty(buffId) ? context.Data.id : buffId, modifiers, duration);
    }
}

[Serializable]
public class HealAction : ArtifactAction
{
    public float flat;
    [Tooltip("Phan tram mau toi da. 20 = +20% health.")]
    public float percentOfMaxHealth = 20f;
    
    public override void Execute(ArtifactContext context)
    {
        float amount = flat + percentOfMaxHealth / 100f * context.Caster.TotalHealth;
        if(amount > 0f) context.Caster.Heal(amount);
    }
}

// Don danh dang cau tai vi tri chuot (targeting = Point).
// Sat thuong = flat + scale x TotalDamage.
[Serializable]
public class AreaStrikeAction : ArtifactAction
{
    public DamageSpec damage = new DamageSpec();
    public float radius = 3f;

    [Tooltip("Do tre luc kich hoat toi luc gay sat thuong (de khop VFX roi xuong).")]
    public float delay = 0.3f;
    public DotEffectSpec effect = new DotEffectSpec();
    public GameObject vfxPrefab;
    public float vfxLifeTime = 2f;
    
    public override void Execute(ArtifactContext context)
    {
        Vector3 point = context.AimPoint;
        int amount = damage.Compute(context.Caster.TotalDamage);

        if (vfxPrefab != null)
        {
            var vfx = Object.Instantiate(vfxPrefab, point, Quaternion.identity);
            Object.Destroy(vfx, vfxLifeTime);
        }

        context.Runner.StartCoroutine(Strike(context, point, amount));
    }

    private IEnumerator Strike(ArtifactContext context, Vector3 point, int amount)
    {
        if(delay > 0f) yield return new WaitForSeconds(delay);
        ArtifactDamage.Area(point, radius, amount, context.Caster, context.EnemyLayer, effect);
    }
}

// Ban 1 hoac nhieu projectile theo huong chuot: luong nang luong, luoi dao, mui ten (+ hieu ung/vu no)
[Serializable]
public class ProjectileAction : ArtifactAction
{
    
    
    public override void Execute(ArtifactContext context)
    {
        
    }
}