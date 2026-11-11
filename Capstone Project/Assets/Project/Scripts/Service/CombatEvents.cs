using System;
using UnityEngine;

/// <summary>
/// Lightweight global combat notifications. Gameplay code raises them,
/// listeners (combat, music, UI, ...)
/// subscribe.
/// </summary>
public static class CombatEvents
{
    // victim, attacker (null for DoT ticks / unknown sources)
    public static event Action<CharacterRuntime, ICharacterRuntime, float> DamageDealt;

    /// An AI calls this with (this, true) when it starts targeting the player and (this, false)
    /// when it loses aggro, dies or gets disabled.
    public static event Action<object, bool> AggroChanged;
    
    public static void RaiseDamageDealt(CharacterRuntime victim, ICharacterRuntime attacker, float damage) => DamageDealt?.Invoke(victim, attacker, damage);
    public static void RaiseAggroChanged(object source, bool hasAggro) => AggroChanged?.Invoke(source, hasAggro);
    
    // Clear them on every play mode session.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        DamageDealt = null;
        AggroChanged = null;
    }
}