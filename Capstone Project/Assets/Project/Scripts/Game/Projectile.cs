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
            Debug.Log($"[Ranged Attack]: Attacked {other.gameObject.name} with {_damage} damage");
        }
        Destroy(gameObject);
    }
}