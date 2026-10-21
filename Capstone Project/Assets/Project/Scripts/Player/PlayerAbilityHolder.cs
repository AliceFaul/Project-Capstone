using System;
using UnityEngine;
using System.Collections.Generic;
using ImprovedTimers;

public class PlayerAbilityHolder : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<Ability> abilities;
    [SerializeField] private AbilityCooldownChannel cooldownChannel;

    private class AbilitySlot
    {
        public Ability Ability { get; }
        public CountdownTimer CooldownTimer { get; }
        public int SlotIndex { get; }

        public AbilitySlot(Ability ability, int slotIndex, Action onTimerFinished)
        {
            Ability = ability;
            SlotIndex = slotIndex;
            
            CooldownTimer = new CountdownTimer(ability != null ? ability.AbilityCooldown : 0f);
            CooldownTimer.OnTimerStop += () => onTimerFinished?.Invoke();
        }
    }
    
    private readonly List<AbilitySlot> _slots = new List<AbilitySlot>();
    public IReadOnlyList<Ability> Abilities => abilities.AsReadOnly();

    private void Awake()
    {
        _slots.Clear();

        for (int i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            if(ability == null) continue;
            
            ability.InitializePool();

            int slotIndex = i;
            var slot = new AbilitySlot(ability, slotIndex, () =>
            {
                cooldownChannel?.AbilityReady(slotIndex);
            });
            
            _slots.Add(slot);
        }
    }

    private void OnEnable()
    {
        foreach (var ability in abilities)
        {
            if (ability != null && ability.Input != null)
            {
                ability.Input.Enable();
            }
        }
    }

    private void OnDisable()
    {
        foreach (var ability in abilities)
        {
            if (ability != null && ability.Input != null)
            {
                ability.Input.Disable();
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].CooldownTimer.Tick();
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var action = slot.Ability?.Input;
            if (action != null && action.WasPressedThisFrame()) AbilityTriggered(i);
        }
    }

    private bool AbilityTriggered(int index)
    {
        if(index < 0 || index >= _slots.Count) return false;
        
        var slot = _slots[index];
        
        if(slot.CooldownTimer.IsRunning)
        {
            Debug.Log($"[PlayerAbilityHolder] {slot.Ability.AbilityName} is on cooldown ({slot.CooldownTimer.CurrentTime:F1}s left)");
            return false;
        }

        StartCoroutine(slot.Ability.ActivateCoroutine(gameObject));
        slot.CooldownTimer.Reset(slot.Ability.AbilityCooldown);
        slot.CooldownTimer.Start();
        
        cooldownChannel?.CooldownStarted(index, slot.Ability.AbilityCooldown, slot.Ability.AbilityCooldown);
        return true;
    }

    public bool GetCooldownConfig(int index, out float remaining, out float max)
    {
        remaining = 0f;
        max = 0f;
        
        if(index < 0 || index >= _slots.Count) return false;

        var slot = _slots[index];
        remaining = slot.CooldownTimer.CurrentTime;
        max = slot.Ability.AbilityCooldown;
        
        return slot.CooldownTimer.IsRunning;
    }
}