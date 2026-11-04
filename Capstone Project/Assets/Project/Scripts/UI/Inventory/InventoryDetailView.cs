using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public class InventoryDetailView : MonoBehaviour
{
    private static readonly Color EmptyColor = new Color(0.08f, 0.17f, 0.23f, 1f);
    private static readonly Color LockedColor = new Color(0.08f, 0.17f, 0.23f, 0.45f);

    [SerializeField] private CanvasGroup canvasGroup;
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

    public void Hide()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void Reveal()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    public void ShowInfo(string header, string title, string body)
    {
        Reveal();
        headerText.text = header;
        Clear();
        SetText(nameText, title);
        SetText(descText, body);
    }

    public void Show(IInventoryItem item, ItemData def, string header, EquipmentManager equip)
    {
        Reveal();
        headerText.text = header;
        Clear();

        var rarity = def != null ? def.rarity : Rarity.Common;
        levelRow.SetActive(true);
        levelText.gameObject.SetActive(false);
        rarityBg.color = InventoryUIUtil.RarityColor(rarity);
        rarityText.text = InventoryUIUtil.RarityName(rarity);

        SetText(nameText, def != null ? (def.itemName ?? "").ToUpper() : InventoryText.Get("DETAIL_UNKNOWN_ITEM"));
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
        levelText.text = InventoryText.Get("DETAIL_LEVEL", e.level);

        bool isWeapon = e.category == InventoryCategory.Melee || e.category == InventoryCategory.Ranged;
        string kind = e.category == InventoryCategory.Armor
            ? InventoryText.Get("DETAIL_SUB_ARMOR")
            : InventoryText.Get(e.category == InventoryCategory.Melee ? "DETAIL_KIND_MELEE" : "DETAIL_KIND_RANGED");
        string weaponType = isWeapon && def != null ? InventoryText.Get("WEAPON_" + def.weaponType.ToString().ToUpper()) : null;
        SetText(subText, weaponType != null ? $"{kind} - {weaponType}" : kind);

        if (cfg != null)
        {
            expGroup.SetActive(true);
            if (e.level >= cfg.maxLevel)
            {
                InventoryUIUtil.SetBar(expFill, 1f);
                expText.text = InventoryText.Get("DETAIL_MAX_LEVEL");
            }
            else
            {
                float need = cfg.GetExpToNextLevel(e.level);
                InventoryUIUtil.SetBar(expFill, e.exp / need);
                expText.text = InventoryText.Get("DETAIL_EXP", e.exp.ToString("0"), need.ToString("0"));
            }
        }

        if (def != null)
        {
            foreach (var pair in StatResolver.GetAllStats(e, def, cfg))
            {
                var table = def.GetStatTable(pair.Key);
                string tier = table != null && table.Tiered ? InventoryText.Get("DETAIL_TIER", e.GetTier(pair.Key) + 1) : "";
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
            }
            else
            {
                socketSquares[i].color = unlocked ? EmptyColor : LockedColor;
                int need = i < cfg.socketUnlockLevels.Length ? cfg.socketUnlockLevels[i] : 0;
                socketLabels[i].text = unlocked ? "" : InventoryText.Get("DETAIL_LEVEL", need);
            }
        }

        SetText(runeText, ""); // rune details are read in the rune socket popup
    }

    // Artifact: cooldown + how it works (numbers in the description are highlighted).
    private void ShowArtifact(ArtifactData def)
    {
        if (def == null) return;

        SetText(subText, InventoryText.Get("DETAIL_SUB_ARTIFACT"));
        AddRow(InventoryText.Get("DETAIL_COOLDOWN"), InventoryText.Get("DETAIL_SECONDS", def.cooldown.ToString("0.#")), "");
        statsGroup.SetActive(true);
        SetText(descText, InventoryUIUtil.HighlightNumbers(def.itemDescription));
    }

    private void ShowRune(RuneInstance rune, RuneData def)
    {
        if (def == null) return;

        SetText(subText, InventoryText.Get(def.kind == RuneKind.Stat ? "DETAIL_SUB_RUNE_STAT" : "DETAIL_SUB_RUNE_EFFECT"));
        AddRow(InventoryText.Get("DETAIL_QUALITY"), $"{RuneResolver.GetQuality(rune, def) * 100f:0}%", "");

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
            AddRow(InventoryText.Get("DETAIL_EFFECT_DAMAGE", def.effectType), $"{dmg:0.#} / {def.tickInterval:0.#}s", "");
            AddRow(InventoryText.Get("DETAIL_DURATION"), InventoryText.Get("DETAIL_SECONDS", dur.ToString("0.#")), "");
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