using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatMusicTrigger : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private MusicTrack combatTrack;
    [SerializeField] private MusicPriority priority = MusicPriority.Combat;
    [SerializeField, Min(0f)] private float enterFade = 0.5f;
    [SerializeField, Min(0f)] private float exitFade = 2f;

    [Tooltip("Also switch the mixer snapshot")]
    [SerializeField] private bool useCombatSnapshot = true;
    
    [Header("Detection")]
    [Tooltip("Damage dealt to characters on these layers counts as combat.")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Seconds without damage or aggro before leaving combat.")]
    [SerializeField, Min(0f)] private float graceSeconds = 5f;
    
    public bool InCombat { get; private set; }
    public event Action<bool> OnCombatChanaged;

    private readonly HashSet<object> _threats = new();
    private float _lastActivityTime = float.NegativeInfinity;

    private static readonly Predicate<object> IsGone = source =>
        source == null
        || (source is UnityEngine.Object unityObj && unityObj == null)
        || source is Component { gameObject: { activeInHierarchy: false } };

    private void OnEnable()
    {
        CombatEvents.DamageDealt += OnDamageDealt;
        CombatEvents.AggroChanged += OnAggroChanged;
    }

    private void OnDisable()
    {
        CombatEvents.DamageDealt -= OnDamageDealt;
        CombatEvents.AggroChanged -= OnAggroChanged;
        
        if(gameObject.scene.isLoaded) LeaveCombat();
    }

    private void Update()
    {
        if(!InCombat) return;

        if(_threats.Count > 0) _threats.RemoveWhere(IsGone);
        
        // Scaled time on purpose: a paused game (timeScale 0) must not drift out of combat.
        if(_threats.Count == 0 && Time.time - _lastActivityTime >= graceSeconds) LeaveCombat();
    }

    public void Ping()
    {
        _lastActivityTime = Time.time;
        EnterCombat();
    }

    // For boss rooms / map changes. If already in combat this crossfades to the new track.
    public void SetCombatTrack(MusicTrack track, MusicPriority newPriority = MusicPriority.Combat)
    {
        combatTrack = track;
        priority = newPriority;
        
        if(!InCombat) return;
        
        if(combatTrack != null) AudioService.Instance?.PushMusic(combatTrack, priority, this, enterFade);
        else AudioService.Instance?.PopMusic(this, exitFade);
    }

    private void OnDamageDealt(CharacterRuntime victim, ICharacterRuntime attacker, float damage)
    {
        if(victim == null) return;
        if(victim is IPlayerRuntime || IsEnemy(victim.gameObject)) Ping();
    }

    private void OnAggroChanged(object source, bool hasAggro)
    {
        if(source == null) return;

        if (hasAggro)
        {
            _threats.Add(source);
            _lastActivityTime = Time.time;
            EnterCombat();
        }
        else if (_threats.Remove(source))
        {
            // The grace period starts when the last aggro source is gone.
            if(_threats.Count == 0) _lastActivityTime = Time.time;
        }
    }

    private bool IsEnemy(GameObject go) => (enemyLayer.value & (1 << go.layer)) != 0;

    private void EnterCombat()
    {
        if(InCombat) return;
        
        InCombat = true;
        if(combatTrack != null) AudioService.Instance?.PushMusic(combatTrack, priority, this, enterFade);
        if(useCombatSnapshot) AudioService.Instance?.SetSnapshot(AudioSnapshotType.Combat, enterFade);
        OnCombatChanaged?.Invoke(true);
    }

    private void LeaveCombat()
    {
        if(!InCombat) return;
        
        InCombat = false;
        _threats.Clear();
        
        AudioService.Instance?.PopMusic(this, exitFade);
        if(useCombatSnapshot) AudioService.Instance?.SetSnapshot(AudioSnapshotType.Normal, exitFade);
        OnCombatChanaged?.Invoke(false);
    }
}