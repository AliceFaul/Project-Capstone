using UnityEngine;

/// Rest node: click to heal a percentage of the player's max health (one use per visit).
public class RestStation : Interactable
{
    [Range(0f, 1f)] [SerializeField] private float healPercent = 0.5f;

    [Header("Presentation")]
    [SerializeField] private ParticleSystem healVfx;
    [SerializeField] private AudioCue healCue;
    [Tooltip("FloatingTextContainer id for '+HP'. Empty = console log only.")]
    [SerializeField] private string floatingTextPrefabId;
    [SerializeField] private GameObject readyVisual;
    [SerializeField] private GameObject usedVisual;

    private bool _used;

    private void OnEnable() => RefreshVisual();

    public override bool CanInteract(PlayerController player)
    {
        if (_used || !base.CanInteract(player)) return false;

        var runtime = player != null ? player.PlayerRuntime : null;
        return runtime != null && !runtime.IsMaxHealth;
    }

    protected override void OnInteract(PlayerController player)
    {
        var runtime = player.PlayerRuntime;
        float amount = runtime.TotalHealth * healPercent;
        runtime.Heal(amount);
        _used = true;

        if (healVfx != null) healVfx.Play();
        if (healCue != null) AudioService.Instance?.PlaySfx(healCue, transform.position);
        WorldMessage.Show(floatingTextPrefabId, InventoryText.Get("REST_HEALED", Mathf.RoundToInt(amount)), player.transform.position + Vector3.up * 1.5f);
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (readyVisual != null) readyVisual.SetActive(!_used);
        if (usedVisual != null) usedVisual.SetActive(_used);
    }
}