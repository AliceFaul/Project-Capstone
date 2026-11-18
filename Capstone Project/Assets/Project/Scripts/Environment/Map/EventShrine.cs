using System;
using UnityEngine;

/// Event node building block: pay a cost (gold, Gem, a share of max health), maybe gamble, get a reward.
/// Covers blood altars, gold shrines, healing fountains, gambles... just by changing the numbers in the Inspector.
public class EventShrine : Interactable, IAreaResettable
{
    [Serializable]
    public class Cost
    {
        [Min(0)] public int gold;
        [Min(0)] public int gem;
        [Tooltip("Share of max health paid (armor still reduces it). You cannot pay with your last health.")]
        [Range(0f, 1f)] public float healthPercent;
    }

    [Serializable]
    public class Reward
    {
        [Min(0)] public int gold;
        [Min(0)] public int gem;
        [Range(0f, 1f)] public float healPercent;
        [Tooltip("Optional: items (and the table's own gold / Gem) given on success.")]
        public LootTable lootTable;
    }

    [SerializeField] private Cost cost = new Cost();
    [SerializeField] private Reward reward = new Reward();
    [Range(0f, 1f)] [SerializeField] private float successChance = 1f;
    [Tooltip("Share of max health lost when the gamble fails.")]
    [Range(0f, 1f)] [SerializeField] private float failDamagePercent;

    [Header("Presentation")]
    [SerializeField] private WorldPickup pickupPrefab;
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private ParticleSystem successVfx;
    [SerializeField] private ParticleSystem failVfx;
    [SerializeField] private AudioCue successCue;
    [SerializeField] private AudioCue failCue;
    [SerializeField] private string floatingTextPrefabId;
    [SerializeField] private GameObject readyVisual;
    [SerializeField] private GameObject usedVisual;

    private bool _used;

    private void OnEnable() => RefreshVisual();

    public override bool CanInteract(PlayerController player) => !_used && base.CanInteract(player);

    public void ResetForEnter()
    {
        _used = false;
        RefreshVisual();
    }

    protected override void OnInteract(PlayerController player)
    {
        var runtime = player.PlayerRuntime;
        var currency = runtime != null ? runtime.Currency : null;
        if (runtime == null || currency == null) return;

        Vector3 messagePosition = player.transform.position + Vector3.up * 1.5f;

        if (currency.Gold() < cost.gold) { Say("EVENT_NOT_ENOUGH_GOLD", messagePosition); return; }
        if (currency.Gem() < cost.gem) { Say("EVENT_NOT_ENOUGH_GEM", messagePosition); return; }

        int healthCost = Mathf.RoundToInt(runtime.TotalHealth * cost.healthPercent);
        if (healthCost > 0 && runtime.Health <= healthCost) { Say("EVENT_NOT_ENOUGH_HP", messagePosition); return; }

        // pay
        if (cost.gold > 0) currency.TrySpend(CurrencyType.Gold, cost.gold);
        if (cost.gem > 0) currency.TrySpend(CurrencyType.Gem, cost.gem);
        if (healthCost > 0) runtime.TakeDamage(healthCost);
        _used = true;

        var rng = new System.Random();
        if (rng.NextDouble() <= successChance) GiveReward(player, runtime, currency, rng, messagePosition);
        else Fail(runtime, messagePosition);

        RefreshVisual();
    }

    private void GiveReward(PlayerController player, PlayerRuntime runtime, Currency currency, System.Random rng, Vector3 messagePosition)
    {
        if (reward.gold > 0) currency.Add(CurrencyType.Gold, reward.gold);
        if (reward.gem > 0) currency.Add(CurrencyType.Gem, reward.gem);
        if (reward.healPercent > 0f) runtime.Heal(runtime.TotalHealth * reward.healPercent);

        if (reward.lootTable != null)
        {
            LootService.GrantCurrency(reward.lootTable, player, rng);
            var drops = LootService.Roll(reward.lootTable, LootService.Difficulty, rng);
            Vector3 origin = dropOrigin != null ? dropOrigin.position : transform.position + Vector3.up * 0.5f;
            LootService.Spawn(drops, origin, pickupPrefab);
        }

        if (successVfx != null) successVfx.Play();
        if (successCue != null) AudioService.Instance?.PlaySfx(successCue, transform.position);
        Say("EVENT_SUCCESS", messagePosition);
    }

    private void Fail(PlayerRuntime runtime, Vector3 messagePosition)
    {
        int damage = Mathf.RoundToInt(runtime.TotalHealth * failDamagePercent);
        if (damage > 0 && runtime.Health > 1) runtime.TakeDamage(Mathf.Min(damage, runtime.Health - 1));

        if (failVfx != null) failVfx.Play();
        if (failCue != null) AudioService.Instance?.PlaySfx(failCue, transform.position);
        Say("EVENT_FAILED", messagePosition);
    }

    private void Say(string key, Vector3 position) => WorldMessage.Show(floatingTextPrefabId, InventoryText.Get(key), position);

    private void RefreshVisual()
    {
        if (readyVisual != null) readyVisual.SetActive(!_used);
        if (usedVisual != null) usedVisual.SetActive(_used);
    }
}