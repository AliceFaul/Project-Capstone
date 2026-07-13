using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private int damage = 10;
    
    Vector3 _direction;
    
    public void Initialize(Vector3 dir)
    {
        _direction = dir.normalized;
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
            attackable.TakeDamage(damage);
            Debug.Log($"[Ranged Attack]: Attacked {other.gameObject.name} with {damage} damage");
        }
        Destroy(gameObject);
    }
}