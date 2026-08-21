using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    
    private Vector3 _direction;
    private int _damage;
    
    public void Initialize(Vector3 dir, int damage)
    {
        _direction = dir.normalized;
        _damage = damage;
        Destroy(gameObject, 3f);
    }

    private void FixedUpdate()
    {
        transform.position += _direction * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IAttackable attackable))
        {
            attackable.TakeDamage(_damage);
            CreateDamagePopup(other, _damage);
            Debug.Log($"[Ranged Attack]: Attacked {other.gameObject.name} with {_damage} damage");
        }
        Destroy(gameObject);
    }
    
    private void CreateDamagePopup(Collider damageable, float damage)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError($"[Projectile] UIManager instance is null");
            return;
        }
        
        var floatingTextService = UIManager.Instance.GetFloatingTextService();
        if (floatingTextService == null)
        {
            Debug.LogError($"[Projectile] FloatingText service is null]");
        }

        string instanceId = $"dmg_{damageable.GetInstanceID()}_{Time.frameCount}_{Random.Range(0, 9999)}";
        var position = damageable.bounds.center + Vector3.up * (damageable.bounds.extents.y * 0.5f);
        
        floatingTextService?.Create("DamageText", 
            instanceId, 
            damage.ToString("0"), 
            position);
    }
}