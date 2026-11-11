using System.Collections.Generic;
using System.Text;
using Project.Capstone.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

// CONG CU TEST (cung nhom voi DevLoadoutGiver): in trang thai de doi chieu voi bo testcase
// va thuc hien cac thao tac ma UI chua co (thao do, gan/go rune, cong EXP, tu gay sat thuong...).
// Gan len 1 GameObject trong scene. Dung: F1 = Dump State, hoac chuot phai tieu de component > menu.
// Chi chay o Editor / Development Build. Can da chen cac patch Phase B, C (PlayerRuntime.ModifyStat...).
public class DevTestActions : MonoBehaviour
{
    [Header("Tham so cho cac menu")]
    [SerializeField] private EquipSlot targetSlot = EquipSlot.Melee;
    [Tooltip("0..2 - socket cho menu Insert/Remove Rune.")]
    [SerializeField, Range(0, 2)] private int socketIndex = 0;
    [SerializeField] private float expAmount = 150f;
    [SerializeField] private float selfDamage = 20f;
    [SerializeField] private float selfHeal = 20f;
    [SerializeField] private int gemAmount = 1000;

    private static readonly BonusStat[] WeaponStats =
    {
        BonusStat.Damage, BonusStat.AttackSpeed, BonusStat.AttackRange, BonusStat.CritChance, BonusStat.CritDamage
    };

    private PlayerRuntime _player;
    private PlayerBuffs _buffs;
    private ArtifactController _artifacts;
    private EquipmentManager _equipment;
    private PlayerDataConfig _data;

    private void Update()
    {
        if (!Debug.isDebugBuild) return;
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) DumpState();
    }

    private bool Bind()
    {
        if (!Debug.isDebugBuild) return false;

        _player = FindFirstObjectByType<PlayerRuntime>();
        _buffs = FindFirstObjectByType<PlayerBuffs>();
        _artifacts = FindFirstObjectByType<ArtifactController>();
        _equipment = EquipmentManager.Instance;

        try
        {
            if (StartupProcessor.Instance != null
                && StartupProcessor.Instance.GetService(out ConfigManager configManager))
            {
                configManager.GetConfig(out _data);
            }
        }
        catch (System.Exception)
        {
            _data = null;
        }

        if (_player == null || _equipment == null || _data == null)
        {
            Debug.LogError("[DevTestActions] Chua san sang: can dang Play, co PlayerRuntime, EquipmentManager va PlayerDataConfig.", this);
            return false;
        }

        return true;
    }

    // ===================== Dump =====================

    [ContextMenu("Dump State (F1)")]
    public void DumpState()
    {
        if (!Bind()) return;

        var config = _equipment.ProgressConfig;
        var sb = new StringBuilder();
        sb.AppendLine("========== DEV STATE ==========");

        sb.AppendLine($"[PLAYER] HP {_player.Health}/{_player.TotalHealth} | Damage {_player.TotalDamage} | Armor {_player.TotalArmor} | " +
                      $"Speed {_player.TotalSpeed:0.##} | Crit {_player.TotalCritChance}% / {_player.TotalCritDamage}%");

        foreach (var slot in new[] { EquipSlot.Melee, EquipSlot.Ranged })
        {
            float weaponSpeed = _equipment.GetStat(slot, BonusStat.AttackSpeed);
            sb.AppendLine($"[ATTACK SPEED {slot}] vu khi {weaponSpeed:0.###} -> sau buff nguoi choi {_player.ModifyStat(BonusStat.AttackSpeed, weaponSpeed):0.###}");
        }

        sb.AppendLine("[EQUIPPED]");
        foreach (var slot in new[] { EquipSlot.Melee, EquipSlot.Ranged, EquipSlot.Armor })
            DumpEquipment(sb, slot, config);

        for (int i = 0; i < Loadout.ArtifactSlotCount; i++)
        {
            var instance = _equipment.GetArtifact(i);
            var data = _equipment.GetArtifactDefinition(instance);
            string cooldown = _artifacts != null ? $"{_artifacts.GetCooldownRemaining(i):0.0}s" : "(khong co ArtifactController)";
            sb.AppendLine(data == null
                ? $"  Artifact {i + 1}: (trong)"
                : $"  Artifact {i + 1}: {data.itemName} [{data.group}/{data.targeting}] cooldown con {cooldown}");
        }

        sb.AppendLine("[BUFFS]");
        bool anyBuff = false;
        if (_buffs != null)
        {
            foreach (var pair in _buffs.GetActive())
            {
                sb.AppendLine($"  {pair.Key}: con {pair.Value:0.0}s");
                anyBuff = true;
            }
        }
        if (!anyBuff) sb.AppendLine("  (khong co)");

        sb.AppendLine("[BAGS]");
        foreach (InventoryCategory category in System.Enum.GetValues(typeof(InventoryCategory)))
        {
            int capacity = _data.Inventory.GetCapacity(category);
            sb.AppendLine($"  {category}: {capacity - _data.Inventory.GetFreeSlots(category)}/{capacity}");
        }

        sb.AppendLine($"[CURRENCY] Gold {_data.Currency.Gold()} | Gem {_data.Currency.Gem()}");
        sb.AppendLine("===============================");
        Debug.Log(sb.ToString(), this);
    }

    private void DumpEquipment(StringBuilder sb, EquipSlot slot, EquipmentProgressConfig config)
    {
        var instance = _equipment.GetEquipped(slot);
        var definition = _equipment.GetDefinition(instance);

        if (instance == null)
        {
            sb.AppendLine($"  {slot}: (trong)");
            return;
        }

        var tiers = new List<string>();
        foreach (var roll in instance.rolls) tiers.Add($"{roll.stat}:T{roll.tier + 1}");

        sb.AppendLine($"  {slot}: {(definition != null ? definition.itemName : "??? khong tra cuu duoc definition")} " +
                      $"id={Short(instance.instanceId)} lv={instance.level} exp={instance.exp:0.#} tier=[{string.Join(", ", tiers)}]");

        if (definition == null) return;

        var stats = new List<string>();
        if (slot == EquipSlot.Armor)
        {
            foreach (var pair in StatResolver.GetAllStats(instance, definition, config))
                stats.Add($"{pair.Key}={pair.Value:0.###}");
        }
        else
        {
            foreach (var stat in WeaponStats) stats.Add($"{stat}={_equipment.GetStat(slot, stat):0.###}");
        }
        sb.AppendLine($"      chi so: {string.Join(", ", stats)}");

        if (slot == EquipSlot.Armor) return;

        var active = RuneResolver.GetActiveRunes(instance, config.GetRune);
        for (int i = 0; i < EquipmentInstance.SocketCount; i++)
        {
            var rune = instance.GetSocket(i);
            string unlocked = config.IsSocketUnlocked(instance.level, i) ? "mo" : "KHOA";

            if (rune == null || rune.IsEmpty)
            {
                sb.AppendLine($"      socket {i + 1} [{unlocked}]: (trong)");
                continue;
            }

            var runeData = config.GetRune(rune.runeId);
            bool isActive = active.Exists(a => a.SocketIndex == i);
            var rolls = new List<string>();
            foreach (var roll in rune.rolls) rolls.Add($"{roll.id}={roll.value}");

            string quality = runeData != null ? $"{RuneResolver.GetQuality(rune, runeData):0.00}" : "?";
            sb.AppendLine($"      socket {i + 1} [{unlocked}]: {(runeData != null ? runeData.itemName : rune.runeId)} " +
                          $"{(isActive ? "ACTIVE" : "BI LOAI (trung nhom)")} quality={quality} rolls=[{string.Join(", ", rolls)}]");
        }
    }

    private static string Short(string id) => string.IsNullOrEmpty(id) ? "-" : id.Substring(0, Mathf.Min(6, id.Length));

    // ===================== Thao do =====================

    [ContextMenu("Unequip Target Slot")]
    public void UnequipTarget()
    {
        if (!Bind()) return;
        Debug.Log($"[DevTest] Unequip {targetSlot} -> {(_equipment.Unequip(targetSlot) ? "OK" : "THAT BAI (trong hoac tui day)")}", this);
    }

    [ContextMenu("Unequip Artifact (socketIndex)")]
    public void UnequipArtifact()
    {
        if (!Bind()) return;
        Debug.Log($"[DevTest] Unequip Artifact {socketIndex + 1} -> {(_equipment.Unequip(EquipSlot.Artifact, socketIndex) ? "OK" : "THAT BAI")}", this);
    }

    // ===================== Rune / socket =====================

    [ContextMenu("Insert First Rune In Bag -> Target Weapon Socket")]
    public void InsertRune()
    {
        if (!Bind()) return;

        var weapon = _equipment.GetEquipped(targetSlot);
        RuneInstance rune = null;
        foreach (var item in _data.Inventory.GetSlots(InventoryCategory.Rune))
        {
            if (item is RuneInstance candidate) { rune = candidate; break; }
        }

        var result = _equipment.Sockets.TryInsert(weapon, socketIndex, rune);
        Debug.Log($"[DevTest] Insert rune vao {targetSlot} socket {socketIndex + 1} -> {result}", this);
    }

    [ContextMenu("Remove Rune From Target Weapon Socket")]
    public void RemoveRune()
    {
        if (!Bind()) return;

        var weapon = _equipment.GetEquipped(targetSlot);
        int gemBefore = _data.Currency.Gem();
        int freeBefore = _data.Inventory.GetFreeSlots(InventoryCategory.Rune);

        var result = _equipment.Sockets.TryRemove(weapon, socketIndex);
        Debug.Log($"[DevTest] Remove rune {targetSlot} socket {socketIndex + 1} -> {result} | " +
                  $"Gem {gemBefore} -> {_data.Currency.Gem()} (phi du kien {_equipment.Sockets.GetRemovalCost(weapon)}) | " +
                  $"o trong tui Rune {freeBefore} -> {_data.Inventory.GetFreeSlots(InventoryCategory.Rune)}", this);
    }

    // ===================== Level / tien / mau =====================

    [ContextMenu("Add EXP To Target Equipment")]
    public void AddExp()
    {
        if (!Bind()) return;

        var instance = _equipment.GetEquipped(targetSlot);
        if (instance == null)
        {
            Debug.LogWarning($"[DevTest] {targetSlot} dang trong.", this);
            return;
        }

        int levelBefore = instance.level;
        int gained = EquipmentLeveling.AddExp(instance, expAmount, _equipment.ProgressConfig);
        _equipment.NotifyStatsChanged(instance);
        Debug.Log($"[DevTest] +{expAmount} EXP: level {levelBefore} -> {instance.level} (+{gained}), exp du {instance.exp:0.#}", this);
    }

    [ContextMenu("Add Gem")]
    public void AddGem()
    {
        if (!Bind()) return;
        _data.Currency.Add(CurrencyType.Gem, gemAmount);
        Debug.Log($"[DevTest] +{gemAmount} Gem -> {_data.Currency.Gem()}", this);
    }

    [ContextMenu("Damage Self")]
    public void DamageSelf()
    {
        if (!Bind()) return;
        int before = _player.Health;
        _player.TakeDamage(selfDamage);
        Debug.Log($"[DevTest] Tu nhan {selfDamage} sat thuong (Armor {_player.TotalArmor}): HP {before} -> {_player.Health}", this);
    }

    [ContextMenu("Heal Self")]
    public void HealSelf()
    {
        if (!Bind()) return;
        int before = _player.Health;
        _player.Heal(selfHeal);
        Debug.Log($"[DevTest] Hoi {selfHeal}: HP {before} -> {_player.Health}/{_player.TotalHealth}", this);
    }

    [ContextMenu("Reset Artifact Cooldowns")]
    public void ResetCooldowns()
    {
        if (!Bind()) return;
        if (_artifacts != null) _artifacts.ResetCooldown();
        Debug.Log("[DevTest] Da reset cooldown artifact.", this);
    }
}
