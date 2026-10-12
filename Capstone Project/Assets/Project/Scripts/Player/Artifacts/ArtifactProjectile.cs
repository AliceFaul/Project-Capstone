using System.Collections.Generic;
using UnityEngine;

public struct ProjectileLaunch
{
    public Vector3 Direction;
    public float Speed;
    public float Lifetime;
    public int Damage;
    public int Pierce;
    public PlayerRuntime Caster;
    public DotEffectSpec Effect;
    public ExplosionSpec Explosion;
    public LayerMask EnemyLayer;
    public LayerMask ObstacleLayer;
}

// Projectile rieng cua artifact. Tu quet va cham bang SphereCast
// nen prefab khong can Collider/Rigidbody - chi can model/VFX.
public class ArtifactProjectile : MonoBehaviour
{
    [SerializeField] private float radius = 0.3f;
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private float hitVfxLifetime = 2f;

    private ProjectileLaunch _launch;
    private float _lifeLeft;
    private int _pierceLeft;
    private bool _launched;
    private readonly HashSet<IAttackable> _hit = new();

    public void Launch(ProjectileLaunch launch)
    {
        _launch = launch;
    }
}