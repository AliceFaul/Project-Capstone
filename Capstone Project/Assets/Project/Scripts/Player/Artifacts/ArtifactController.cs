using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArtifactController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform castOrigin;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Audio")]
    [Tooltip("Played when an artifact key is pressed while that artifact is still on cooldown. " +
             "Give the cue a cooldown of ~0.3s so holding / spamming the key does not machine-gun it.")]
    [SerializeField] private AudioCue onCooldownCue;

    private struct Cooldown
    {
        public float readyAt;
        public float duration;
    }

    private readonly Dictionary<string, Cooldown> _cooldowns = new Dictionary<string, Cooldown>();

    private InputHandler _inputHandler;
    private EquipmentManager _equipment;
    private PlayerRuntime _player;
    private PlayerBuffs _buffs;
    private bool _isCasting;

    public bool IsCasting => _isCasting;

    public event Action<int, ArtifactData> OnArtifactUsed;

    // A dead player must not be able to use (or finish casting) an artifact.
    private bool IsPlayerDead => _player != null && _player.Health <= 0;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (mainCamera == null) mainCamera = Camera.main;

        if (controller != null)
        {
            _player = controller.PlayerRuntime;
            _buffs = controller.PlayerBuffs;
            _inputHandler = controller.InputHandler;
        }

        // Fallback if controller isn't assigned.
        _player ??= GetComponent<PlayerRuntime>();
        _buffs ??= GetComponent<PlayerBuffs>();
        _inputHandler ??= GetComponent<InputHandler>();
    }

    private void Start()
    {
        _equipment = EquipmentManager.Instance;
        if (_equipment == null) Debug.LogError($"[ArtifactController] EquipmentManager.Instance is missing.");
    }

    private void OnEnable()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnArtifact1 += OnArtifact1;
            _inputHandler.OnArtifact2 += OnArtifact2;
            _inputHandler.OnArtifact3 += OnArtifact3;
        }
    }

    private void OnDisable()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnArtifact1 -= OnArtifact1;
            _inputHandler.OnArtifact2 -= OnArtifact2;
            _inputHandler.OnArtifact3 -= OnArtifact3;
        }

        // Coroutines are stopped without running their finally block, so unlock here. Only when we were the one
        // casting: otherwise this would also release a combat lock that belongs to a melee attack.
        if (_isCasting && controller != null) controller.CmdCombatLocked(false);
        _isCasting = false;
    }

    private void OnArtifact1() => TryActivate(0);
    private void OnArtifact2() => TryActivate(1);
    private void OnArtifact3() => TryActivate(2);

    public float GetCooldownRemaining(ArtifactInstance instance)
    {
        if (instance == null || !_cooldowns.TryGetValue(instance.instanceId, out var cooldown)) return 0f;
        return Mathf.Max(0f, cooldown.readyAt - Time.time);
    }

    public float GetCooldownRemaining(int slot)
    {
        return _equipment != null ? GetCooldownRemaining(_equipment.GetArtifact(slot)) : 0f;
    }

    public float GetCooldownNormalized(int slot)
    {
        var instance = _equipment != null ? _equipment.GetArtifact(slot) : null;
        if (instance == null || !_cooldowns.TryGetValue(instance.instanceId, out var cooldown) || cooldown.duration <= 0f) return 0f;
        return Mathf.Clamp01(GetCooldownRemaining(instance) / cooldown.duration);
    }

    // Dung khi test: xoa het cooldown de thu lai ngay.
    public void ResetCooldown() => _cooldowns.Clear();

    public bool TryActivate(int slot)
    {
        if (_equipment == null || _isCasting) return false;
        if (IsPlayerDead) return false;

        var instance = _equipment.GetArtifact(slot);
        var data = _equipment.GetArtifactDefinition(instance);
        if (instance == null || data == null) return false;

        if (GetCooldownRemaining(instance) > 0f)
        {
            AudioService.Instance?.PlaySfx(onCooldownCue);
            return false;
        }

        if (data.LocksMovement && (controller == null || !controller.PlayerModifier.CanAttack)) return false;
        if (!TryBuildContext(data, out var context)) return false;

        _cooldowns[instance.instanceId] = new Cooldown { readyAt = Time.time + data.cooldown, duration = data.cooldown };
        OnArtifactUsed?.Invoke(slot, data);
        PlayUseSounds(data, context);

        if (data.LocksMovement) StartCoroutine(CastRoutine(data, context));
        else Execute(data, context);
        return true;
    }

    private bool TryBuildContext(ArtifactData data, out ArtifactContext context)
    {
        Vector3 origin = castOrigin != null ? castOrigin.position : transform.position + Vector3.up;
        Vector3 aimPoint = transform.position;
        Vector3 aimDirection = transform.forward;

        if (data.targeting != ArtifactTargeting.Seft)
        {
            var mouse = Mouse.current;
            if (mouse == null || mainCamera == null)
            {
                context = null;
                return false;
            }

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 200f, groundLayer))
            {
                context = null;
                return false;
            }

            Vector3 flat = hit.point - transform.position;
            flat.y = 0f;

            aimDirection = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward;
            aimPoint = hit.point;

            if (data.targeting == ArtifactTargeting.Point && flat.magnitude > data.maxRange)
            {
                Vector3 clamped = transform.position + aimDirection * data.maxRange;
                aimPoint = new Vector3(clamped.x, hit.point.y, clamped.z);
            }
        }

        context = new ArtifactContext
        {
            Data = data,
            Caster = _player,
            CasterTransform = transform,
            Origin = origin,
            AimPoint = aimPoint,
            AimDirection = aimDirection,
            Buffs = _buffs,
            Runner = this,
            EnemyLayer = enemyLayer,
            ObstacleLayer = obstacleLayer
        };
        return true;
    }

    private IEnumerator CastRoutine(ArtifactData data, ArtifactContext context)
    {
        // Checked BEFORE _isCasting is set: with no controller this used to leave _isCasting = true forever
        // and block every artifact from then on.
        if (controller == null) yield break;

        _isCasting = true;
        controller.CmdCombatLocked(true);
        controller.Movement.Stop();

        if (data.targeting != ArtifactTargeting.Seft) transform.rotation = Quaternion.LookRotation(context.AimDirection);
        if (animator != null && !string.IsNullOrEmpty(data.animationTrigger)) animator.SetTrigger(data.animationTrigger);

        try
        {
            float executeAt = Mathf.Clamp(data.executeTime, 0f, data.castTime);
            if (executeAt > 0f) yield return new WaitForSeconds(executeAt);

            if (IsPlayerDead) yield break;

            Execute(data, context);

            float remaining = data.castTime - executeAt;
            if (remaining > 0f) yield return new WaitForSeconds(remaining);
        }
        finally
        {
            if (controller != null) controller.CmdCombatLocked(false);
            _isCasting = false;
        }
    }

    // Sounds marked "OnUse" (charge-up) play the moment the key is pressed, before the cast time.
    private static void PlayUseSounds(ArtifactData data, ArtifactContext context)
    {
        if (data.actions == null) return;

        foreach (var action in data.actions)
        {
            if (action is PlaySfxAction { PlaysOnUse: true } sfx) sfx.Play(context);
        }
    }

    private void Execute(ArtifactData data, ArtifactContext context)
    {
        if (data.actions == null) return;

        foreach (var action in data.actions)
        {
            if (action == null) continue;

            try
            {
                action.Execute(context);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
    }
}