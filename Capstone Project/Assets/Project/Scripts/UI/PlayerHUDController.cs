using System;
using Project.Capstone.Inventory;
using UnityEngine;

public class PlayerHUDController : MonoBehaviour
{
    private const int ArtifactSlotCount = 3;
    
    [Header("HUD View")]
    [SerializeField] private PlayerHUDView view;
    [SerializeField] private CurrencyUI currencyUI;
    
    [Header("Player")]
    [SerializeField] private PlayerController controller;
    
    [Header("Ability Cooldown")]
    [SerializeField] private AbilityCooldownChannel cooldownChannel;

    private EquipmentManager _equipment;
    private PlayerRuntime _runtime;
    private PlayerAbilityHolder _abilityHolder;
    private ArtifactController _artifactController;

    private bool[] _abilityCooling = Array.Empty<bool>();
    private readonly bool[] _artifactCooling = new bool[ArtifactSlotCount];

    private bool _started;
    private bool _isBound;

    private void Awake()
    {
        ResolveComponents();
    }

    private void Start()
    {
        ResolveComponents();
        _started = true;
        Bind();
    }

    private void OnEnable()
    {
        if(!_started) return;
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        if(!_isBound) return;
        UpdateAbilityCooldowns();
        UpdateArtifactCooldowns();
    }

    private void ResolveComponents()
    {
        if(view == null) view = GetComponent<PlayerHUDView>();
        if(controller == null) controller = GetComponent<PlayerController>() ?? FindFirstObjectByType<PlayerController>();

        if (controller != null)
        {
            if(_runtime == null) _runtime = controller.PlayerRuntime;
            if(_abilityHolder == null) _abilityHolder = controller.AbilityHolder;
            if(_artifactController == null) _artifactController = controller.ArtifactController;
        }
        else
        {
            Debug.LogError($"[PlayerHUDController] Can't found PlayerController.");
        }
        
        if(_equipment == null) _equipment = EquipmentManager.Instance;
    }

    private void Bind()
    {
        if(_isBound) return;
        ResolveComponents();

        if (view == null)
        {
            Debug.LogError($"[PlayerHUDController] View is missing.", this);
            return;
        }

        if (_runtime != null)
        {
            _runtime.OnHpChanged += OnHpChanged;
            _runtime.OnStatsChanged += OnStatsChanged;
            _runtime.OnExpChanged += OnExpChanged;
            _runtime.OnLevelUp += OnLevelUp;
        }
        else
        {
            Debug.LogError($"[PlayerHUDController] PlayerRuntime is missing. Can't find Player.", this);
        }

        if (cooldownChannel != null)
        {
            cooldownChannel.OnCooldownStarted += OnAbilityCooldownStarted;
            cooldownChannel.OnAbilityReady += OnAbilityReady;
        }
        
        if(_artifactController != null) _artifactController.OnArtifactUsed += OnArtifactUsed;
        else Debug.LogError($"[PlayerHUDController] ArtifactController is missing. Need assign Controller.", this);

        if (_equipment != null)
        {
            _equipment.OnEquipmentChanged += OnEquipmentChanged;
            _equipment.OnLoadoutReloaded += RefreshArtifacts;
        }
        
        if(currencyUI != null && _runtime != null) currencyUI.Initialize(_runtime);
        
        _isBound = true;
        RefreshAll();
    }

    private void Unbind()
    {
        if(!_isBound) return;

        if (_runtime != null)
        {
            _runtime.OnHpChanged -= OnHpChanged;
            _runtime.OnStatsChanged -= OnStatsChanged;
            _runtime.OnExpChanged -= OnExpChanged;
            _runtime.OnLevelUp -= OnLevelUp;
        }

        if (cooldownChannel != null)
        {
            cooldownChannel.OnCooldownStarted -= OnAbilityCooldownStarted;
            cooldownChannel.OnAbilityReady -= OnAbilityReady;
        }
        
        if(_artifactController != null) _artifactController.OnArtifactUsed -= OnArtifactUsed;

        if (_equipment != null)
        {
            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
            _equipment.OnLoadoutReloaded -= RefreshArtifacts;
        }

        _isBound = false;
    }

    private void RefreshAll()
    {
        RefreshHealth();
        RefreshProgression();
        RefreshAbilities();
        RefreshArtifacts();
    }

    private void OnHpChanged(int current)
    {
        if(_runtime == null || view == null) return;
        view.SetHealth(current, _runtime.TotalHealth);
    }

    private void OnStatsChanged() => RefreshHealth();
    private void OnExpChanged(float current, float required) => view?.SetExperience(current, required);

    private void OnLevelUp(int newLevel)
    {
        view?.SetLevel(newLevel);
        RefreshProgression();
    }

    private void RefreshHealth()
    {
        if(_runtime == null || view == null) return;
        view.SnapHealth(_runtime.Health, _runtime.TotalHealth);
    }

    private void RefreshProgression()
    {
        if(_runtime == null || view == null) return;

        int level = _runtime.Config != null ? _runtime.Config.Level : 1;
        view.SetLevel(level);
        view.SnapExperience(_runtime.CurrentExp, _runtime.ExpToNextLevel);
    }
    
    private void RefreshAbilities()
    {
        if(_abilityHolder == null || view == null) return;
        
        var abilities = _abilityHolder.Abilities;
        if(abilities == null) return;
        
        if(_abilityCooling.Length != abilities.Count) _abilityCooling = new bool[abilities.Count];

        for (int i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            view.SetAbilityIcon(i, ability != null ? ability.AbilityIcon : null);

            if (_abilityHolder.GetCooldownConfig(i, out float remaining, out float maximum))
            {
                _abilityCooling[i] = true;
                view.SetAbilityCooldown(i, maximum > 0f ? remaining / maximum : 0f);
            }
            else
            {
                _abilityCooling[i] = false;
                view.SetAbilityCooldown(i, 0f);
            }
        }
    }

    private void OnAbilityCooldownStarted(int slot, float remaining, float maximum)
    {
        if(slot < 0 || slot >= _abilityCooling.Length) return;
        _abilityCooling[slot] = maximum > 0f;
        view?.SetAbilityCooldown(slot, maximum > 0f ? remaining / maximum : 0f);
    }

    private void OnAbilityReady(int slot)
    {
        if(slot < 0 || slot >= _abilityCooling.Length) return;
        _abilityCooling[slot] = false;
        view?.SetAbilityCooldown(slot, 0f);
    }

    private void UpdateAbilityCooldowns()
    {
        if(_abilityHolder == null || view == null) return;

        for (int i = 0; i < _abilityCooling.Length; i++)
        {
            if(!_abilityCooling[i]) continue;

            if (_abilityHolder.GetCooldownConfig(i, out float remaining, out float maximum))
            {
                view.SetAbilityCooldown(i, maximum > 0f ? remaining / maximum : 0f);
            }
            else
            {
                _abilityCooling[i] = false;
                view.SetAbilityCooldown(i, 0f);
            }
        }
    }

    private void RefreshArtifacts()
    {
        if(view == null) return;
        for(int i = 0; i < ArtifactSlotCount; i++) RefreshArtifactSlot(i);
    }

    private void RefreshArtifactSlot(int slot)
    {
        if(slot is < 0 or >= ArtifactSlotCount) return;

        Sprite icon = null;
        float cooldown = 0f;

        if (_equipment != null)
        {
            var instance = _equipment.GetArtifact(slot);
            var definition = _equipment.GetArtifactDefinition(instance);
            
            if(definition != null) icon = definition.icon;
        }
        
        if(_artifactController != null) cooldown = _artifactController.GetCooldownNormalized(slot);
        
        _artifactCooling[slot] = cooldown > 0.001f;
        
        view.SetArtifactIcon(slot, icon);
        view.SetArtifactCooldown(slot, cooldown);
    }

    private void OnArtifactUsed(int slot, ArtifactData data)
    {
        if(slot is < 0 or >= ArtifactSlotCount) return;
        _artifactCooling[slot] = true;
        if(_artifactController != null) view?.SetArtifactCooldown(slot, _artifactController.GetCooldownNormalized(slot));
    }

    private void UpdateArtifactCooldowns()
    {
        if(_artifactController == null || view == null) return;

        for (int i = 0; i < ArtifactSlotCount; i++)
        {
            if(!_artifactCooling[i]) continue;
            
            float normalized = _artifactController.GetCooldownNormalized(i);
            view.SetArtifactCooldown(i, normalized);
            
            if(normalized <= 0.001f) _artifactCooling[i] = false;
        }
    }

    private void OnEquipmentChanged(EquipmentChangedEventArgs args)
    {
        if(args == null) return;
        if(args.Slot == EquipSlot.Artifact) RefreshArtifactSlot(args.Index);
    }
}