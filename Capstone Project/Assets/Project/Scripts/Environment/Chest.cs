using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// Click to open: plays the open animation, rolls the loot table and makes the items pop out.
public class Chest : Interactable
{
    [Header("Loot")]
    [SerializeField] private LootTable table;
    [SerializeField] private WorldPickup pickupPrefab;
    
    [Tooltip("Where items pop out from. Empty = above the chest.")]
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private float scatterRadius = 1.6f;
    [SerializeField] private int difficultyOverride = -1;
    
    [Header("Presentation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    
    [Tooltip("Seconds between the click and the items appearing.")]
    [SerializeField] private float openDelay = 0.6f;
    [SerializeField] private ParticleSystem openVfx;
    [SerializeField] private AudioCue openCue;

    [SerializeField] private UnityEvent onOpened;
    
    public bool IsOpen { get; private set; }
    public event Action<Chest> Opened;
    
    public override bool CanInteract(PlayerController player) => !IsOpen && base.CanInteract(player);

    protected override void OnInteract(PlayerController player)
    {
        if(IsOpen) return;
        IsOpen = true;
        StartCoroutine(OpenRoutine(player));
    }

    public void ResetChest()
    {
        StopAllCoroutines();
        IsOpen = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if(animator == null) return;
        animator.Rebind();
        animator.Update(0f);
    }
    
    private IEnumerator OpenRoutine(PlayerController player)
    {
        if (table == null)
        {
            Debug.LogWarning($"[Chest] {name} has no LootTable assigned.", this);
            yield break;
        }

        if (pickupPrefab == null)
        {
            Debug.LogError($"[Chest] {name} has no WorldPickup prefab assigned.", this);
            yield break;
        }
        
        if(animator != null && !string.IsNullOrEmpty(openTrigger)) animator.SetTrigger(openTrigger);
        if(openCue != null) AudioService.Instance?.PlaySfx(openCue, transform.position);
        if(openVfx != null) openVfx.Play();
        
        if(openDelay > 0f) yield return new WaitForSeconds(openDelay);
        
        var rng = new System.Random();
        int difficulty = difficultyOverride >= 0 ? difficultyOverride : LootService.Difficulty;
        var drops = LootService.Roll(table, difficulty, rng);
        
        LootService.GrantCurrency(table, player, rng);
        Vector3 origin = dropOrigin != null ? dropOrigin.position : transform.position + Vector3.up * 0.5f;
        LootService.Spawn(drops, origin, pickupPrefab, scatterRadius);
        
        Opened?.Invoke(this);
        onOpened?.Invoke();
    }
}