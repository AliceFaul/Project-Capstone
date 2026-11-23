using System;
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

public class ArtifactProjectile : MonoBehaviour
{
    [SerializeField] private float radius = 0.3f;
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private float hitVfxLifetime = 2f;

    [Header("Audio")]
    [Tooltip("Optional. Loops while the projectile flies (whoosh, hum) and follows it.")]
    [SerializeField] private AudioCue flightLoopSfx;
    [Tooltip("Optional. Played where the projectile hits something, and when it pierces through an enemy.")]
    [SerializeField] private AudioCue hitSfx;

    // A single frame step is short, so 32 hits is far more than can ever overlap.
    private const int HitBufferSize = 32;
    private static readonly HitDistanceComparer DistanceComparer = new HitDistanceComparer();

    private ProjectileLaunch _launch;
    private float _lifeLeft;
    private int _pierceLeft;
    private bool _launched;
    private int _loopHandle;
    private readonly HashSet<IAttackable> _hit = new();
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];

    private sealed class HitDistanceComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }

    public void Launch(ProjectileLaunch launch)
    {
        _launch = launch;
        _launch.Direction = launch.Direction.normalized;
        _lifeLeft = launch.Lifetime;
        _pierceLeft = launch.Pierce;
        _launched = true;

        transform.rotation = Quaternion.LookRotation(_launch.Direction);

        foreach (var coll in Physics.OverlapSphere(transform.position, radius, _launch.EnemyLayer))
        {
            if (HandleEnemy(coll)) continue;
            Finish(transform.position);
            return;
        }

        StartFlightLoop();
    }

    private void Update()
    {
        if (!_launched) return;

        float step = _launch.Speed * Time.deltaTime;
        LayerMask mask = _launch.EnemyLayer | _launch.ObstacleLayer;

        // NonAlloc: SphereCastAll allocated a new array for every projectile on every frame.
        int count = Physics.SphereCastNonAlloc(transform.position, radius, _launch.Direction, _hitBuffer, step, mask, QueryTriggerInteraction.Ignore);
        if (count > 0)
        {
            // Sort by distance so no enemy inside this frame's flight path is skipped when piercing.
            Array.Sort(_hitBuffer, 0, count, DistanceComparer);

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                bool isEnemy = (_launch.EnemyLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
                if (isEnemy && HandleEnemy(hit.collider)) continue;

                Vector3 point = hit.point == Vector3.zero ? transform.position : hit.point;
                Finish(point);
                return;
            }
        }

        transform.position += _launch.Direction * step;
        _lifeLeft -= Time.deltaTime;
        if (_lifeLeft <= 0f) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        StopFlightLoop();
    }

    private bool HandleEnemy(Collider coll)
    {
        var attackable = coll.GetComponentInParent<IAttackable>();
        if (!IsValidTarget(attackable) || !_hit.Add(attackable)) return true;

        ArtifactDamage.Hit(attackable, _launch.Damage, _launch.Caster, _launch.Effect);

        if (_pierceLeft > 0)
        {
            _pierceLeft--;
            AudioService.Instance?.PlaySfx(hitSfx, transform.position); // passing through: Finish() is not called
            return true;
        }

        return false;
    }

    private void Finish(Vector3 point)
    {
        _launched = false;
        StopFlightLoop();

        var explosion = _launch.Explosion;

        if (explosion is { enabled: true })
        {
            int damage = Mathf.RoundToInt(_launch.Damage * explosion.damageScale);
            ArtifactDamage.Area(point, explosion.radius, damage, _launch.Caster, _launch.EnemyLayer, _launch.Effect, _hit);

            if (explosion.vfxPrefab)
            {
                var vfx = Instantiate(explosion.vfxPrefab, point, Quaternion.identity);
                Destroy(vfx, explosion.vfxLifeTime);
            }

            AudioService.Instance?.PlaySfx(explosion.sfx, point);
        }

        if (hitVfx)
        {
            var vfx = Instantiate(hitVfx, point, Quaternion.identity);
            Destroy(vfx, hitVfxLifetime);
        }

        AudioService.Instance?.PlaySfx(hitSfx, point);

        Destroy(gameObject);
    }

    private void StartFlightLoop()
    {
        if (flightLoopSfx == null || AudioService.Instance == null) return;

        // Safety net: even if Finish() is never reached the loop fades out shortly after the projectile's lifetime.
        _loopHandle = AudioService.Instance.PlayLoop(flightLoopSfx, transform, _launch.Lifetime + 0.5f);
    }

    private void StopFlightLoop()
    {
        if (_loopHandle == 0) return;

        AudioService.Instance?.StopLoop(_loopHandle, 0.1f);
        _loopHandle = 0;
    }

    private bool IsValidTarget(IAttackable target)
    {
        return target switch
        {
            null => false,
            _ => true
        };
    }
}