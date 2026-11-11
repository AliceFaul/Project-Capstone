using System;
using System.Collections;
using System.Collections.Generic;
using Project.Capstone.Inventory;
using UnityEngine;

// CONG CU TEST: cap trang bi / rune / artifact / tien cho nguoi choi ngay khi vao Play Mode,
// cau hinh hoan toan trong Inspector. Chi hoat dong o Editor va Development Build
// (Debug.isDebugBuild); ban Release tu huy component nay.
//
// YEU CAU: vao Play tu luong Bootstrap/MainMenu de StartupProcessor chay xong ConfigStep (PlayerDataConfig,
// EquipmentProgressConfig phai nam trong ConfigReferenceList). Mo thang scene Gameplay thi khong co config.
// LUU Y: do cap nam trong tui that nen neu game save luc dang co do test thi chung vao file save/PlayFab.
// Dung tai khoan test; "Cleanup On Destroy" go het do da cap khi thoat Play de han che viec nay.

public enum TierPick { Random, Tier1, Tier2, Tier3, Tier4 }

[Serializable]
public class DevEquipmentGrant
{
    public EquipmentData equipment;
    [Range(1, 10)] public int level = 10;
    [Tooltip("So ban sao cung cap (dung de test phan bo tier Random). Neu equip = true thi ban cuoi cung duoc mac.")]
    [Min(1)] public int copies = 1;
    [Tooltip("Tier cua cac chi so co tier. Random = nhu luc nhat do that.")]
    public TierPick tier = TierPick.Random;
    public bool equip = true;
    [Tooltip("Rune gan san vao socket 1..3 (chi vu khi). De trong = socket trong. Bo qua dieu kien mo khoa theo level.")]
    public RuneData[] socketRunes = new RuneData[EquipmentInstance.SocketCount];
    public RollBias runeRoll = RollBias.Random;
}

[Serializable]
public class DevRuneGrant
{
    public RuneData rune;
    [Min(1)] public int count = 1;
    public RollBias roll = RollBias.Random;
}

[Serializable]
public class DevArtifactGrant
{
    public ArtifactData artifact;
    public bool equip = true;
    [Tooltip("-1 = o trong dau tien (het cho thi o 1).")]
    [Range(-1, 2)] public int slot = -1;
}

public class DevLoadoutGiver : MonoBehaviour
{
    [Header("Khi nao cap")]
    [SerializeField] private bool grantOnStart = true;
    [Tooltip("Cho toi da bao lau (giay) de startup/config san sang.")]
    [SerializeField] private float waitTimeout = 30f;
    [Tooltip("Cap lai moi khi du lieu duoc nap lai tu save/cloud (nap cloud xong se xoa sach do vua cap).")]
    [SerializeField] private bool regrantWhenDataApplied = true;
    [Tooltip("Go het do da cap khi object nay bi huy (thoat Play / doi scene).")]
    [SerializeField] private bool cleanupOnDestroy = true;

    [Header("Trang bi (vu khi / giap)")]
    [SerializeField] private List<DevEquipmentGrant> equipment = new List<DevEquipmentGrant>();

    [Header("Rune bo vao tui")]
    [SerializeField] private List<DevRuneGrant> runes = new List<DevRuneGrant>();

    [Header("Artifact")]
    [SerializeField] private List<DevArtifactGrant> artifacts = new List<DevArtifactGrant>();

    [Header("Tien (tru lai khi cleanup)")]
    [SerializeField] private int gold;
    [SerializeField] private int gem;

    private struct Replaced
    {
        public EquipSlot slot;
        public int index;
        public IInventoryItem previous;
    }

    private PlayerDataConfig _data;
    private EquipmentProgressConfig _progress;
    private EquipmentManager _equipment;
    private bool _bound;

    private readonly List<IInventoryItem> _granted = new List<IInventoryItem>();
    private readonly List<Replaced> _replaced = new List<Replaced>();
    private int _grantedGold;
    private int _grantedGem;

    private IEnumerator Start()
    {
        if (!Debug.isDebugBuild)
        {
            Destroy(this);
            yield break;
        }

        if (!grantOnStart) yield break;

        float waited = 0f;
        while (!TryBind())
        {
            waited += Time.unscaledDeltaTime;
            if (waited >= waitTimeout)
            {
                Debug.LogError("[DevLoadoutGiver] Het thoi gian cho. Hay vao Play tu Bootstrap/MainMenu (StartupProcessor phai chay) " +
                               "va kiem tra PlayerDataConfig + EquipmentProgressConfig da co trong ConfigReferenceList.", this);
                yield break;
            }
            yield return null;
        }

        Grant();
    }

    private void OnDestroy()
    {
        if (!_bound) return;

        _data.OnDataApplied -= HandleDataApplied;
        if (cleanupOnDestroy) Cleanup();
    }

    private bool TryBind()
    {
        if (_bound) return true;

        try
        {
            if (StartupProcessor.Instance == null) return false;
            if (!StartupProcessor.Instance.GetService(out ConfigManager configManager)) return false;
            if (!configManager.GetConfig(out PlayerDataConfig data)) return false;
            if (!configManager.GetConfig(out EquipmentProgressConfig progress)) return false;
            if (EquipmentManager.Instance == null) return false;

            _data = data;
            _progress = progress;
            _equipment = EquipmentManager.Instance;
            _data.OnDataApplied += HandleDataApplied;
            _bound = true;
            return true;
        }
        catch (Exception)
        {
            // ServiceRegistry chua duoc tao (startup chua toi buoc do).
            return false;
        }
    }

    // Du lieu vua bi nap de len (cloud/local) -> do da cap truoc do khong con nua, cap lai.
    private void HandleDataApplied()
    {
        if (!regrantWhenDataApplied) return;

        _granted.Clear();
        _replaced.Clear();
        _grantedGold = 0;
        _grantedGem = 0;
        Grant();
    }

    // Sua danh sach trong Inspector luc dang Play roi bam "..." > Regrant de cap lai ma khong can chay lai game.
    [ContextMenu("Regrant")]
    public void Grant()
    {
        if (!Debug.isDebugBuild || !TryBind())
        {
            Debug.LogError("[DevLoadoutGiver] Chua san sang (can dang Play, startup xong).", this);
            return;
        }

        Cleanup(); // lam lai tu dau

        var rng = new System.Random();
        foreach (var grant in equipment)
        {
            int copies = grant != null ? Mathf.Max(1, grant.copies) : 1;
            for (int i = 0; i < copies; i++) GrantEquipment(grant, rng);
        }
        foreach (var grant in runes) GrantRunes(grant, rng);
        foreach (var grant in artifacts) GrantArtifact(grant);
        GrantCurrency();

        Debug.Log($"[DevLoadoutGiver] Da cap {_granted.Count} vat pham, +{_grantedGold} Gold, +{_grantedGem} Gem.", this);
    }

    [ContextMenu("Cleanup")]
    public void Cleanup()
    {
        if (!_bound)
        {
            ClearTracking();
            return;
        }

        var loadout = _data.Loadout;
        var inventory = _data.Inventory;

        foreach (var item in _granted)
        {
            // Dang mac thi thao ve tui truoc, roi xoa khoi tui.
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                int count = slot == EquipSlot.Artifact ? Loadout.ArtifactSlotCount : 1;
                for (int i = 0; i < count; i++)
                {
                    if (loadout.Get(slot, i) == item && !loadout.TryUnequip(slot, i))
                        Debug.LogWarning($"[DevLoadoutGiver] Khong thao duoc {item.InstanceId} (tui {item.Category} day?).", this);
                }
            }

            inventory.TryRemove(item.InstanceId, out _);
        }

        // Tra lai do that truoc do o nhung slot da bi do test thay the.
        foreach (var replaced in _replaced)
        {
            if (loadout.Get(replaced.slot, replaced.index) != null) continue;
            if (inventory.Find(replaced.previous.InstanceId) == null) continue;
            loadout.TryEquip(replaced.previous, replaced.index);
        }

        if (_grantedGold > 0) _data.Currency.TrySpend(CurrencyType.Gold, _grantedGold);
        if (_grantedGem > 0) _data.Currency.TrySpend(CurrencyType.Gem, _grantedGem);

        ClearTracking();
    }

    private void ClearTracking()
    {
        _granted.Clear();
        _replaced.Clear();
        _grantedGold = 0;
        _grantedGem = 0;
    }

    // ===================== Cap tung loai =====================

    private void GrantEquipment(DevEquipmentGrant grant, System.Random rng)
    {
        if (grant == null || grant.equipment == null) return;
        WarnIfNotInDatabase(grant.equipment);

        var instance = LootRoller.RollEquipment(grant.equipment, _progress, rng);
        if (instance == null) return;

        instance.level = Mathf.Clamp(grant.level, 1, _progress.maxLevel);
        ApplyTier(instance, grant.tier);

        bool isWeapon = instance.category == InventoryCategory.Melee || instance.category == InventoryCategory.Ranged;
        for (int i = 0; i < EquipmentInstance.SocketCount && grant.socketRunes != null && i < grant.socketRunes.Length; i++)
        {
            var runeData = grant.socketRunes[i];
            if (runeData == null) continue;

            if (!isWeapon)
            {
                Debug.LogWarning($"[DevLoadoutGiver] {grant.equipment.name} khong phai vu khi - bo qua rune.", this);
                break;
            }

            WarnIfNotInDatabase(runeData);
            instance.sockets[i] = RuneRoller.Roll(runeData, rng, grant.runeRoll);

            if (!_progress.IsSocketUnlocked(instance.level, i))
                Debug.LogWarning($"[DevLoadoutGiver] Socket {i + 1} cua {grant.equipment.name} chua mo o level {instance.level} (van gan de test).", this);
        }

        if (!_data.Inventory.TryAdd(instance))
        {
            Debug.LogError($"[DevLoadoutGiver] Tui {instance.category} da day, khong cap duoc {grant.equipment.name}.", this);
            return;
        }

        _granted.Add(instance);

        var tierText = new List<string>();
        foreach (var roll in instance.rolls) tierText.Add($"{roll.stat}:T{roll.tier + 1}");
        Debug.Log($"[DevLoadoutGiver] + {grant.equipment.itemName} id={instance.instanceId.Substring(0, 6)} lv={instance.level} tier=[{string.Join(", ", tierText)}]", this);

        if (grant.equip) EquipTracked(instance, -1);
    }

    private void GrantRunes(DevRuneGrant grant, System.Random rng)
    {
        if (grant == null || grant.rune == null) return;
        WarnIfNotInDatabase(grant.rune);

        for (int i = 0; i < Mathf.Max(1, grant.count); i++)
        {
            var rune = RuneRoller.Roll(grant.rune, rng, grant.roll);
            if (!_data.Inventory.TryAdd(rune))
            {
                Debug.LogError("[DevLoadoutGiver] Tui Rune da day.", this);
                return;
            }
            _granted.Add(rune);
        }
    }

    private void GrantArtifact(DevArtifactGrant grant)
    {
        if (grant == null || grant.artifact == null) return;
        WarnIfNotInDatabase(grant.artifact);

        var instance = LootRoller.CreateArtifact(grant.artifact.id);
        if (!_data.Inventory.TryAdd(instance))
        {
            Debug.LogError("[DevLoadoutGiver] Tui Artifact da day.", this);
            return;
        }

        _granted.Add(instance);
        if (grant.equip) EquipTracked(instance, grant.slot);
    }

    private void GrantCurrency()
    {
        if (gold > 0)
        {
            _data.Currency.Add(CurrencyType.Gold, gold);
            _grantedGold = gold;
        }

        if (gem > 0)
        {
            _data.Currency.Add(CurrencyType.Gem, gem);
            _grantedGem = gem;
        }
    }

    // ===================== Helper =====================

    private static void ApplyTier(EquipmentInstance instance, TierPick pick)
    {
        if (pick == TierPick.Random) return;

        int tier = (int)pick - 1; // Tier1 -> 0 ... Tier4 -> 3
        foreach (var roll in instance.rolls) roll.tier = tier;
    }

    private void EquipTracked(IInventoryItem item, int artifactSlot)
    {
        if (!Loadout.TrySlotFor(item, out var slot)) return;

        int index = slot == EquipSlot.Artifact ? ResolveArtifactIndex(artifactSlot) : 0;

        // Ghi lai do that dang mac o slot nay (neu co) de tra lai khi cleanup.
        var previous = _data.Loadout.Get(slot, index);
        bool alreadyTracked = _replaced.Exists(r => r.slot == slot && r.index == index);
        if (previous != null && !_granted.Contains(previous) && !alreadyTracked)
            _replaced.Add(new Replaced { slot = slot, index = index, previous = previous });

        if (!_equipment.Equip(item, slot == EquipSlot.Artifact ? index : -1))
            Debug.LogWarning($"[DevLoadoutGiver] Khong mac duoc {item.InstanceId}.", this);
    }

    private int ResolveArtifactIndex(int requested)
    {
        if (requested >= 0 && requested < Loadout.ArtifactSlotCount) return requested;

        for (int i = 0; i < Loadout.ArtifactSlotCount; i++)
        {
            if (_data.Loadout.GetArtifact(i) == null) return i;
        }
        return 0;
    }

    private void WarnIfNotInDatabase(ItemData definition)
    {
        if (definition == null) return;

        if (_data.GetDefinition<ItemData>(definition.id) == null)
            Debug.LogWarning($"[DevLoadoutGiver] '{definition.name}' (id '{definition.id}') chua co trong ItemDatabase - game se khong tra cuu duoc.", definition);
    }
}
