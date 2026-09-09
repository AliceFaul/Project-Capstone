using UnityEngine;

public enum StatusEffectType
{
    Fire,
    Ice,
    Poison,
    Paralyzed,
    Sleeping,
}

public abstract class StatusEffectData : ScriptableObject
{
    public StatusEffectType type;
}