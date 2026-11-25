using System;
using System.Collections;
using UnityEngine;

// Lives on the Player. Phase 1: the core of dying and coming back, without lives, UI or visual effects yet.
//   - listens to PlayerRuntime.OnDied and stops everything the player was doing,
//   - Revive() puts the player back in a clean, playable state (optionally at another position).
// Later phases drive it from RunManager (lives, respawn delay, dissolve effect, UI).
public class DeathController : MonoBehaviour
{
    [Tooltip("Empty = PlayerController on this object.")]
    [SerializeField] private PlayerController controller;

    [Header("Revive")]
    [Tooltip("Fraction of max health the player comes back with.")]
    [Range(0.05f, 1f)] [SerializeField] private float reviveHealthFraction = 1f;
    [Tooltip("Seconds the player cannot be damaged right after coming back.")]
    [Min(0f)] [SerializeField] private float spawnProtectionSeconds = 3f;
    [Tooltip("Remove the active buffs on revive. Off = buffs keep running.")]
    [SerializeField] private bool clearBuffsOnRevive = false;

    private PlayerRuntime _runtime;
    private Coroutine _protectionRoutine;

    public bool IsDead { get; private set; }

    public event Action OnPlayerDied;
    public event Action OnPlayerRevived;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        _runtime = controller != null ? controller.PlayerRuntime : null;

        if (_runtime != null) _runtime.OnDied += HandleDied;
        else Debug.LogError("[DeathController] PlayerRuntime not found.", this);
    }

    private void OnDisable()
    {
        if (_runtime != null) _runtime.OnDied -= HandleDied;

        // Coroutines stop together with the component, so release the flag the coroutine would have cleared.
        _protectionRoutine = null;
        if (controller != null) controller.PlayerModifier.SetSpawnProtection(false);
    }

    private void HandleDied()
    {
        if (IsDead) return;
        IsDead = true;

        // PlayerRuntime.Die() already raised the dead flag, so input, abilities and attacks are blocked.
        // Here we stop what is already in motion.
        controller.Movement.HaltImmediately();
        controller.Combat.CancelAll();
        EndSpawnProtection();

        var animationHandler = controller.AnimationHandler;
        if (animationHandler != null) animationHandler.FreezePose(true);

        OnPlayerDied?.Invoke();
    }

    // Brings the player back. position: where to appear (null = stay where he died).
    // healthFraction: null = use the value configured above.
    public bool Revive(Vector3? position = null, float? healthFraction = null)
    {
        if (!IsDead || _runtime == null) return false;

        var modifier = controller.PlayerModifier;
        modifier.SetDead(false);
        modifier.MoveModifier(true);
        modifier.AttackModifier(true);

        // Dead -> Locomotion: PlayerAnimationHandler crossfades back to locomotion by itself.
        controller.StateMachine.Revive();

        var animationHandler = controller.AnimationHandler;
        if (animationHandler != null) animationHandler.FreezePose(false);

        _runtime.Revive(healthFraction ?? reviveHealthFraction);

        if (clearBuffsOnRevive)
        {
            var buffs = controller.PlayerBuffs;
            if (buffs != null) buffs.RemoveAll();
        }

        if (position.HasValue) controller.Movement.WarpTo(position.Value);

        IsDead = false;
        StartSpawnProtection();

        OnPlayerRevived?.Invoke();
        return true;
    }

    private void StartSpawnProtection()
    {
        EndSpawnProtection();
        if (spawnProtectionSeconds <= 0f) return;

        _protectionRoutine = StartCoroutine(SpawnProtectionRoutine());
    }

    private void EndSpawnProtection()
    {
        if (_protectionRoutine != null)
        {
            StopCoroutine(_protectionRoutine);
            _protectionRoutine = null;
        }

        controller.PlayerModifier.SetSpawnProtection(false);
    }

    private IEnumerator SpawnProtectionRoutine()
    {
        controller.PlayerModifier.SetSpawnProtection(true);
        yield return new WaitForSeconds(spawnProtectionSeconds);

        controller.PlayerModifier.SetSpawnProtection(false);
        _protectionRoutine = null;
    }
}