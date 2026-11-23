
using Mhieu.Enemy;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DarkProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float lifeTime = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject impactEffect;

    private Rigidbody rb;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Projectile bay bằng Rigidbody.
        rb.useGravity = false;
        rb.isKinematic = false;

        Collider projectileCollider = GetComponent<Collider>();
        projectileCollider.isTrigger = true;
    }

    public void Launch(Vector3 direction)
    {
        direction.y = direction.y;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;

        // Không gây sát thương lên Enemy.
        if (other.GetComponentInParent<EnemyBrain>() != null)
            return;

        if (!other.CompareTag("Player"))
            return;

        hasHit = true;

        // Gọi hàm nhận sát thương của Player.
        other.SendMessageUpwards(
            "TakeDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        if (impactEffect != null)
        {
            Instantiate(
                impactEffect,
                transform.position,
                Quaternion.identity
            );
        }

        Destroy(gameObject);
    }
}

