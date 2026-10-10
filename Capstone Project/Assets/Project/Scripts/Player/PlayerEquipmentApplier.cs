using System;
using Project.Capstone.Inventory;
using UnityEngine;
using System.Collections.Generic;

// Gan tren Player (cung GameObject voi PlayerRuntime). Gom chi so tu trang bi thanh 1 "nguon"
// modifier cho PlayerRuntime moi khi do doi / nap save / level hoac socket doi:
//  - Giap: TAT CA chi so cua giap ap cho nguoi choi (Defense, MoveSpeed, Health...).
//  - Rune tren vu khi: chi dong player-scoped (Health, Defense, MoveSpeed) ap cho nguoi choi.
//    Dong vu khi-scoped (Damage, Crit, AttackSpeed, AttackRange) KHONG di qua day - chung duoc
//    StatResolver tinh theo TUNG vu khi luc danh (tranh cong trung 2 lan, loi cu o DamageCalculator).
public class PlayerEquipmentApplier : MonoBehaviour
{
    public const string SourceId = "equipment";

    private static readonly BonusStat[] PlayerScopedRuneStats =
        { BonusStat.Health, BonusStat.Defense, BonusStat.MoveSpeed };

    private PlayerRuntime _runtime;
    private EquipmentManager _equipmentManager;

    private void Awake()
    {
        _runtime = GetComponent<PlayerRuntime>();
    }

    private void Start()
    {
        _equipmentManager = EquipmentManager.Instance;

        if (_runtime == null || _equipmentManager == null)
        {
            Debug.LogError($"[PlayerEquipmentApplier] Missing PlayerRuntime in this object or Missing EquipmentManager.Instance.");
            return;
        }
        
        _equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
        _equipmentManager.OnLoadoutReloaded += Rebuild;
        _equipmentManager.OnEquipmentStatsChanged += OnStatsChanged;
        
        Rebuild();
    }

    private void OnDestroy()
    {
        if(_equipmentManager == null) return;
        
        _equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
        _equipmentManager.OnLoadoutReloaded -= Rebuild;
        _equipmentManager.OnEquipmentStatsChanged -= OnStatsChanged;
    }

    private void OnEquipmentChanged(EquipmentChangedEventArgs args) => Rebuild();
    private void OnStatsChanged(EquipSlot slot) => Rebuild();

    private void Rebuild()
    {
        var config = _equipmentManager.ProgressConfig;
        var modifiers = new List<StatModifier>();

        var armor = _equipmentManager.GetEquipped(EquipSlot.Armor);
        var armorDefinition = _equipmentManager.GetDefinition(armor);

        if (armor != null && armorDefinition != null)
        {
            foreach (var pair in StatResolver.GetAllStats(armor, armorDefinition, config))
            {
                modifiers.Add(new StatModifier(pair.Key, pair.Value));
            }
        }
        
        AddRuneModifiers(EquipSlot.Melee, config, modifiers);
        AddRuneModifiers(EquipSlot.Ranged, config, modifiers);
    }

    private void AddRuneModifiers(EquipSlot slot, EquipmentProgressConfig config, List<StatModifier> modifiers)
    {
        var weapon = _equipmentManager.GetEquipped(slot);
        if(weapon == null || config == null) return;

        foreach (var stat in PlayerScopedRuneStats)
        {
            float value = RuneResolver.GetStatBonus(weapon, stat, config.GetRune);
            if(value != 0f) modifiers.Add(new StatModifier(stat, value));
        }
    }
}