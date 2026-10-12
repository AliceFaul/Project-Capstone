using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

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
    }

    private void Update()
    {
        if(!_launched) return;
        
        float step = _launch.Speed * Time.deltaTime;
        LayerMask mask = _launch.EnemyLayer | _launch.ObstacleLayer;

        var hits = Physics.SphereCastAll(transform.position, radius, _launch.Direction, step, mask, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                bool isEnemy = (_launch.EnemyLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
                if(isEnemy && HandleEnemy(hit.collider)) continue;
                Vector3 point = hit.point == Vector3.zero ? transform.position : hit.point;
                Finish(point);
                return;
            }
        }
        
        transform.position += _launch.Direction * step;
        _lifeLeft -= Time.deltaTime;
        if(_lifeLeft <= 0f) Destroy(gameObject);
    }

    // True = tiep tuc bay (khong phai enemy, da trung roi hoac con luot xuyen), False = dung lai.
    private bool HandleEnemy(Collider coll)
    {
        var attackable = coll.GetComponentInParent<IAttackable>();
        if(!IsValidTarget(attackable) || !_hit.Add(attackable)) return true;
        
        ArtifactDamage.Hit(attackable, _launch.Damage, _launch.Caster, _launch.Effect);

        if (_pierceLeft > 0)
        {
            _pierceLeft--;
            return true;
        }
        
        return false;
    }
    
    private void Finish(Vector3 point)
    {
        _launched = false;
        var explosion = _launch.Explosion;

        if (explosion is { enabled: true })
        {
            int damage = Mathf.RoundToInt(_launch.Damage * explosion.damageScale);
            // Bo qua quai da trung dan truc tiep de khong tinh sat thuong 2 lan.
            ArtifactDamage.Area(point, explosion.radius, damage, _launch.Caster, _launch.EnemyLayer, _launch.Effect, _hit);

            if (explosion.vfxPrefab)
            {
                var vfx = Instantiate(explosion.vfxPrefab, point, Quaternion.identity);
                Destroy(vfx, explosion.vfxLifeTime);
            }
        }

        if (hitVfx)
        {
            var vfx = Instantiate(hitVfx, point, Quaternion.identity);
            Destroy(hitVfx, hitVfxLifetime);
        }
        
        Destroy(gameObject);
    }

    private bool IsValidTarget(IAttackable target)
    {
        return target switch
        {
            null => false,
            Object obj => obj != null,
            _ => true
        };
    }
}