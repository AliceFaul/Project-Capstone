using System;
using System.Collections.Generic;
using UnityEngine;

// Du lieu 1 lan dung artifact, truyen cho tung Action
public class ArtifactContext
{
    public ArtifactData Data;
    public PlayerRuntime Caster;
    public Transform CasterTransform;
    public Vector3 Origin; // diem xuat phat (castOrigin hoac vi tri nguoi choi)
    public Vector3 AimPoint; // diem tren mat dat duoi con tro chuot (da gioi han maxRange neu targeting = Point)
    public Vector3 AimDirection; // huong ngang, da mormalize
    public PlayerBuffs Buffs;
    public MonoBehaviour Runner; // Dung de chay Coroutine
    public LayerMask EnemyLayer;
    public LayerMask ObstacleLayer;
}

// 1 module hanh vi. Ghep nhieu Action trong ArtifactData.actions de tao artifact moi, khong can viet class
[Serializable]
public abstract class ArtifactAction
{
    public abstract void Execute(ArtifactContext context);
    
    // Cac chi so ma Action nay buff len nguoi choi
    public virtual IEnumerable<BonusStat> BuffStats { get { yield break; } }
}

[Serializable]
public class StatBuffEntry
{
    public BonusStat stat;
    
}