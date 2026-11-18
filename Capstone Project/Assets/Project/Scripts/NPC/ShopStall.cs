using UnityEngine;

// The merchant (or his stall): click to trade. Stock is generated the first time you talk to him after the area was entered,
// so every visit of a shop node has new items. Spawn it as a Prop in a Shop area or place it in the area directly.
public class ShopStall : Interactable, IAreaResettable
{
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private ShopPriceConfig prices;
    
    [Tooltip("Empty = found in the parent areas or the scene.")]
    [SerializeField] private ShopSceneDirector director;
    
    [Tooltip("-1 = use LootService.Difficulty.")]
    [SerializeField] private int difficultyOverride = -1;

    private ShopService _stock;

    public void ResetForEnter() => _stock = null;
    public override bool CanInteract(PlayerController player) => base.CanInteract(player) && (director == null || !director.IsOpen);

    protected override void OnInteract(PlayerController player)
    {
        if (director == null) director = GetComponentInParent<ShopSceneDirector>();
        if (director == null) director = FindFirstObjectByType<ShopSceneDirector>();
        if (director == null || catalog == null || prices == null)
        {
            Debug.LogError("[ShopStall] Needs a ShopCatalog, a ShopPriceConfig and a ShopSceneDirector.", this);
            return;
        }

        var data = player.PlayerRuntime != null ? player.PlayerRuntime.Config : null;
        if (data == null) return;

        int difficulty = difficultyOverride >= 0 ? difficultyOverride : LootService.Difficulty;
        _stock ??= ShopService.Generate(data, catalog, prices, difficulty);

        director.Open(_stock, player);
    }
}