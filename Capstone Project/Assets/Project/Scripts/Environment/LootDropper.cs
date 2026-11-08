using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Mhieu.Enemy.EnemyHealth))]
public class LootDropper : MonoBehaviour
{
    [SerializeField] private LootTable table;
    [SerializeField] private WorldPickup pickupPrefab;
    [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
    [SerializeField] private float scatterRadius = 1.4f;
    [SerializeField] private int difficultyOverride = -1;

    private Mhieu.Enemy.EnemyHealth _health;
    private bool _dropped;

    private void Awake() => _health = GetComponent<Mhieu.Enemy.EnemyHealth>();

    private void OnEnable()
    {
        _dropped = false;
        _health.Died += OnDied;
    }

    private void OnDisable()
    {
        if(_health == null) return;
        _health.Died -= OnDied;
    }

    private void OnDied(Mhieu.Enemy.EnemyHealth health)
    {
        if(_dropped) return;
        _dropped = true;
        
        if(Random.value > dropChance) return;

        int difficulty = difficultyOverride >= 0 ? difficultyOverride : LootService.Difficulty;
        var drops = LootService.Roll(table, difficulty);
        LootService.Spawn(drops, transform.position, pickupPrefab, scatterRadius);
    }
}