using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project.Capstone.Inventory;

// Right-hand panel. Renders equipment / artifact / rune instances using the game's own data.
public class InventoryDetailView : MonoBehaviour
{
    private static readonly Color EmptyColor = new Color(0.08f, 0.17f, 0.23f, 1f);
    private static readonly Color LockedColor = new Color(0.08f, 0.17f, 0.23f, 0.45f);

    [SerializeField] private TMP_Text headerText;
    [SerializeField] private GameObject levelRow;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image rarityBg;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private GameObject expGroup;
    [SerializeField] private RectTransform expFill;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private GameObject statsGroup;
    [SerializeField] private Transform statsRoot;
    [SerializeField] private GameObject statRowPrefab;
    [SerializeField] private GameObject socketsGroup;
    [SerializeField] private Image[] socketSquares;
    [SerializeField] private TMP_Text[] socketLabels;
    [SerializeField] private TMP_Text runeText;
    [SerializeField] private TMP_Text descText;

    public void ShowInfo(string header, string title, string body)
    {
        headerText.text = header;
        Clear();
        SetText(nameText, title);
        SetText(descText, body);
    }

    public void Show(IInventoryItem item, ItemData def, string header, EquipmentManager equip)
    {
        headerText.text = header;
        Clear();

        var rarity = def != null ? def.rarity : Rarity.Common;
        levelRow.SetActive(true);
        levelText.gameObject.SetActive(false);
        rarityBg.color = InventoryUIUtil.RarityColor(rarity);
        rarityText.text = rarity.ToString().ToUpper();

        SetText(nameText, def != null ? (def.itemName ?? "").ToUpper() : "UNKNOWN ITEM");
        SetText(descText, def != null ? def.itemDescription : "");

        switch (item)
        {
            case EquipmentInstance e: ShowEquipment(e, def as EquipmentData, equip); break;
            case ArtifactInstance _: ShowArtifact(def as ArtifactData); break;
            case RuneInstance r: ShowRune(r, def as RuneData); break;
        }
    }

    private void ShowEquipment(EquipmentInstance e, EquipmentData def, EquipmentManager equip)
    {
        var cfg = equip != null ? equip.ProgressConfig : null;

        levelText.gameObject.SetActive(true);
        levelText.text = $"Lv {e.level}";

        string kind = e.category == InventoryCategory.Armor ? "Armor"
            : e.category == InventoryCategory.Melee ? "Melee weapon" : "Ranged weapon";
        bool isWeapon = e.category == InventoryCategory.Melee || e.category == InventoryCategory.Ranged;
        SetText(subText, isWeapon && def != null ? $"{kind} - {def.weaponType}" : kind);

        if (cfg != null)
        {
            expGroup.SetActive(true);
            if (e.level >= cfg.maxLevel)
            {
                InventoryUIUtil.SetBar(expFill, 1f);
                expText.text = "MAX LEVEL";
            }
            else
            {
                float need = cfg.GetExpToNextLevel(e.level);
                InventoryUIUtil.SetBar(expFill, e.exp / need);
                expText.text = $"{e.exp:0} / {need:0} EXP";
            }
        }

        if (def != null)
        {
            foreach (var pair in StatResolver.GetAllStats(e, def, cfg))
            {
                var table = def.GetStatTable(pair.Key);
                string tier = table != null && table.Tiered ? $"Tier {e.GetTier(pair.Key) + 1}" : "";
                AddRow(InventoryUIUtil.StatName(pair.Key), InventoryUIUtil.FormatStat(pair.Key, pair.Value), tier);
            }
        }
        statsGroup.SetActive(statsRoot.childCount > 0);

        if (isWeapon) ShowSockets(e, equip);
    }

    private void ShowSockets(EquipmentInstance e, EquipmentManager equip)
    {
        var cfg = equip != null ? equip.ProgressConfig : null;
        var sockets = equip != null ? equip.Sockets : null;
        if (cfg == null || sockets == null) return;

        socketsGroup.SetActive(true);
        var active = RuneResolver.GetActiveRunes(e, cfg.GetRune);
        var lines = new List<string>();

        for (int i = 0; i < socketSquares.Length; i++)
        {
            bool unlocked = sockets.IsSocketUnlocked(e, i);
            var rune = e.GetSocket(i);
            bool filled = unlocked && rune != null && !rune.IsEmpty;

            if (filled)
            {
                var data = cfg.GetRune(rune.runeId);
                socketSquares[i].color = InventoryUIUtil.RarityColor(data != null ? data.rarity : Rarity.Common);
                socketLabels[i].text = "";

                bool isActive = false;
                foreach (var a in active)
                {
                    if (a.SocketIndex == i) { isActive = true; break; }
                }
                lines.Add(InventoryUIUtil.RuneLine(rune, data) + (isActive ? "" : " (inactive)"));
            }
            else
            {
                socketSquares[i].color = unlocked ? EmptyColor : LockedColor;
                int need = i < cfg.socketUnlockLevels.Length ? cfg.socketUnlockLevels[i] : 0;
                socketLabels[i].text = unlocked ? "" : $"Lv {need}";
            }
        }

        SetText(runeText, string.Join("\n", lines));
    }

    private void ShowArtifact(ArtifactData def)
    {
        if (def == null) return;

        SetText(subText, $"Artifact - {def.group}");
        AddRow("Cooldown", $"{def.cooldown:0.#}s", "");
        AddRow("Cast time", def.castTime > 0f ? $"{def.castTime:0.#}s" : "Instant", "");
        AddRow("Target", def.targeting == ArtifactTargeting.Seft ? "Self" : def.targeting.ToString(), "");
        statsGroup.SetActive(true);
    }

    private void ShowRune(RuneInstance rune, RuneData def)
    {
        if (def == null) return;

        SetText(subText, def.kind == RuneKind.Stat ? "Stat rune" : "Damage effect rune");
        AddRow("Quality", $"{RuneResolver.GetQuality(rune, def) * 100f:0}%", "");

        if (def.kind == RuneKind.Stat)
        {
            foreach (var line in def.statLines)
            {
                if (!RuneResolver.TryGetRoll(rune, line.Id, out float v)) continue;
                AddRow(InventoryUIUtil.StatName(line.stat), line.mode == StatMode.Percent ? $"+{v:0.##}%" : $"+{v:0.##}", "");
            }
        }
        else
        {
            RuneResolver.TryGetRoll(rune, RuneData.DamagePerTickId, out float dmg);
            RuneResolver.TryGetRoll(rune, RuneData.DurationId, out float dur);
            AddRow($"{def.effectType} damage", $"{dmg:0.#} / {def.tickInterval:0.#}s", "");
            AddRow("Duration", $"{dur:0.#}s", "");
        }
        statsGroup.SetActive(true);
    }

    private void Clear()
    {
        levelRow.SetActive(false);
        expGroup.SetActive(false);
        statsGroup.SetActive(false);
        socketsGroup.SetActive(false);
        SetText(subText, "");
        ClearRows();
    }

    private void ClearRows()
    {
        for (int i = statsRoot.childCount - 1; i >= 0; i--)
        {
            var child = statsRoot.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }

    private void AddRow(string label, string value, string tier)
    {
        var row = Instantiate(statRowPrefab, statsRoot);
        var texts = row.GetComponentsInChildren<TMP_Text>();
        texts[0].text = label;
        texts[1].text = value;
        texts[2].text = tier;
    }

    private static void SetText(TMP_Text text, string value)
    {
        text.text = value;
        text.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }
}
