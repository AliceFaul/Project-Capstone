
using UnityEngine;

public class EnemyDarkProjectileAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private DarkProjectile projectilePrefab;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float fireCooldown = 2f;

    private float nextFireTime;

    // Gọi hàm này khi Enemy thực hiện đòn đánh.
    public void FireProjectile()
    {
        if (Time.time < nextFireTime)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null || firePoint == null || projectilePrefab == null)
            return;

        Vector3 targetPosition = player.transform.position
                               + Vector3.up * 0.8f;

        Vector3 direction = targetPosition - firePoint.position;

        if (direction.magnitude > attackRange)
            return;

        DarkProjectile projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        projectile.Launch(direction);

        nextFireTime = Time.time + fireCooldown;
    }
}

