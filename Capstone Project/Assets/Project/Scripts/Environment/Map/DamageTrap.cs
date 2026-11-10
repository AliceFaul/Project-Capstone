using UnityEngine;

/// Floor trap with a cycle: idle -> telegraph (warning) -> active (hurts the player standing in the radius) -> idle...
public class DamageTrap : MonoBehaviour, IAreaResettable
{
    private enum Phase { Idle, Telegraph, Active }

    [Header("Damage")]
    [SerializeField] private float damage = 15f;
    [Tooltip("If higher than the flat damage, this share of the player's max health is used instead.")]
    [Range(0f, 1f)] [SerializeField] private float percentOfMaxHealth;
    [SerializeField] private float hitCooldown = 0.8f;

    [Header("Shape")]
    [SerializeField] private float radius = 1.2f;
    [SerializeField] private float heightTolerance = 1.5f;

    [Header("Cycle")]
    [SerializeField] private bool alwaysActive;
    [SerializeField] private float startDelay;
    [SerializeField] private float idleTime = 2f;
    [SerializeField] private float telegraphTime = 0.5f;
    [SerializeField] private float activeTime = 1.2f;

    [Header("Presentation")]
    [SerializeField] private GameObject idleVisual;
    [SerializeField] private GameObject telegraphVisual;
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private AudioCue activateCue;
    [SerializeField] private AudioCue hitCue;

    private static PlayerController s_player;
    private static float s_nextSearch;

    private Phase _phase;
    private float _phaseEndsAt;
    private float _nextHitAt;

    private void OnEnable() => ResetForEnter();

    public void ResetForEnter()
    {
        SetPhase(alwaysActive ? Phase.Active : Phase.Idle);
        _phaseEndsAt = Time.time + startDelay + idleTime;
        _nextHitAt = 0f;
    }

    private void Update()
    {
        if (!alwaysActive && Time.time >= _phaseEndsAt) Advance();
        if (_phase == Phase.Active) TryHit();
    }

    private void Advance()
    {
        switch (_phase)
        {
            case Phase.Idle:
                SetPhase(Phase.Telegraph);
                _phaseEndsAt = Time.time + telegraphTime;
                break;
            case Phase.Telegraph:
                SetPhase(Phase.Active);
                _phaseEndsAt = Time.time + activeTime;
                if (activateCue != null) AudioService.Instance?.PlaySfx(activateCue, transform.position);
                break;
            default:
                SetPhase(Phase.Idle);
                _phaseEndsAt = Time.time + idleTime;
                break;
        }
    }

    private void TryHit()
    {
        if (Time.time < _nextHitAt) return;

        var player = FindPlayer();
        if (player == null || player.PlayerRuntime == null) return;

        Vector3 delta = player.transform.position - transform.position;
        if (Mathf.Abs(delta.y) > heightTolerance) return;
        delta.y = 0f;
        if (delta.sqrMagnitude > radius * radius) return;

        var runtime = player.PlayerRuntime;
        float amount = Mathf.Max(damage, runtime.TotalHealth * percentOfMaxHealth);
        runtime.TakeDamage(amount);

        _nextHitAt = Time.time + hitCooldown;
        if (hitCue != null) AudioService.Instance?.PlaySfx(hitCue, transform.position);
    }

    private void SetPhase(Phase phase)
    {
        _phase = phase;
        if (idleVisual != null) idleVisual.SetActive(phase == Phase.Idle);
        if (telegraphVisual != null) telegraphVisual.SetActive(phase == Phase.Telegraph);
        if (activeVisual != null) activeVisual.SetActive(phase == Phase.Active);
    }

    private static PlayerController FindPlayer()
    {
        if (s_player != null) return s_player;
        if (Time.unscaledTime < s_nextSearch) return null;

        s_nextSearch = Time.unscaledTime + 1f;
        s_player = FindFirstObjectByType<PlayerController>();
        return s_player;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}