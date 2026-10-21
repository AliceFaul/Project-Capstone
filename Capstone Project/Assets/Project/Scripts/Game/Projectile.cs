using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    
    private Vector3 _direction;
    private int _damage;
    private GameObject _owner;
    
    public void Initialize(Vector3 dir, int damage, GameObject owner = null)
    {
        _direction = dir.normalized;
        _damage = damage;
        _owner = owner;
        Destroy(gameObject, 3f);
    }

    private void FixedUpdate()
    {
        transform.position += _direction * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_owner != null && other.gameObject == _owner) return;
        
        if (other.TryGetComponent(out IAttackable attackable))
        {
            if (other.CompareTag("Player")) return;
            attackable.TakeDamage(_damage);
            Debug.Log($"[Ranged Attack]: Attacked {other.gameObject.name} with {_damage} damage");
        }
        Destroy(gameObject);
    }
}