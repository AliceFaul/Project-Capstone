using System;
using Project.Capstone.Inventory;
using UnityEngine;
using Random = UnityEngine.Random;

/// A dropped item: pops out, hovers (bobbing) and is pulled toward the player when he comes near,
/// then goes into the inventory.
/// It is also an Interactable, so the player can click it.
public class WorldPickup : Interactable
{
    private enum State { Popping, Hovering, Magnet, Collected }
    
    [Header("Visual")]
    [Tooltip("Child that is billboard to the camera and scaled during the pull. Empty = this object.")]
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private SpriteRenderer glowRenderer;
    [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.55f;
    [SerializeField] private ParticleSystem rarityVfx;
    
    [Header("Pop out")]
    [SerializeField] private float popDuration = 0.45f;
    [SerializeField] private float popHeight = 1.1f;
    
    [Header("Hover")]
    [SerializeField] private float hoverHeight = 0.8f;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 2.2f;
    
    [Header("Magnet")]
    [Tooltip("Seconds after landing before the item can be pulled.")]
    [SerializeField] private float armDelay = 0.5f;
    [SerializeField] private float magnetRadius = 4f;
    [SerializeField] private float pickupRadius = 0.6f;
    [SerializeField] private float startSpeed = 2f;
    [SerializeField] private float acceleration = 26f;
    [SerializeField] private float maxSpeed = 18f;
    
    [Header("Feedback")]
    [SerializeField] private AudioCue dropCue;
    [SerializeField] private AudioCue pickupCue;
    [SerializeField] private string floatingTextPrefabId;
    
    [Header("Hand-placed item")]
    [Tooltip("For items placed in the scene by hand")]
    [SerializeField] private ItemData placedItem;
    
    public IInventoryItem Item { get; private set; }
    public ItemData Definition { get; private set; }

    private static PlayerController s_player;
    private static float s_nextSearch;
    
    private State _state = State.Popping;
    private Vector3 _start;
    private Vector3 _landing;
    private float _t;
    private float _popTime;
    private float _phase;
    private float _armedAt;
    private float _speed;
    private float _nextFullMessage;
    private Vector3 _baseScale = Vector3.one;
    private Camera _camera;

    private void Awake()
    {
        if(visual == null) visual = transform;
        _baseScale = visual.localScale;
    }

    private void Start()
    {
        if(Item != null || placedItem == null) return;

        var config = EquipmentManager.Instance != null ? EquipmentManager.Instance.ProgressConfig : null;
        var item = LootService.CreateItem(placedItem, config, new System.Random());

        if (item == null)
        {
            Destroy(gameObject);
            return;
        }
        Setup(item, placedItem, transform.position);
    }

    public void Setup(IInventoryItem item, ItemData definition, Vector3 landingPoint)
    {
        Item = item;
        Definition = definition;
        _start = transform.position;
        _landing = landingPoint;
        _t = 0f;
        _popTime = Mathf.Max(0.05f, popDuration * Random.Range(0.85f, 1.15f));
        _phase = Random.value * Mathf.PI * 2f;
        _armedAt = Time.time + _popTime + armDelay;
        _state = State.Popping;
        
        ApplyVisual();
        if(dropCue != null) AudioService.Instance?.PlaySfx(dropCue, transform.position);
    }

    public override bool CanInteract(PlayerController player) => base.CanInteract(player) && Item != null && _state is State.Hovering or State.Magnet;
    protected override void OnInteract(PlayerController player) => TryCollect(player);

    private void Update()
    {
        if(Item == null) return;

        switch (_state)
        {
            case State.Popping: UpdatePop(); break;
            case State.Hovering: UpdateHover(); break;
            case State.Magnet: UpdateMagnet(); break;
        }
    }

    private void LateUpdate()
    {
        if(_camera == null) _camera = Camera.main;
        if(_camera != null && iconRenderer != null) visual.rotation = _camera.transform.rotation;
    }

    private void UpdatePop()
    {
        _t += Time.deltaTime / _popTime;
        float k = Mathf.Clamp01(_t);
        Vector3 target = _landing + Vector3.up * hoverHeight;
        transform.position = Vector3.Lerp(_start, target, k) + Vector3.up * (popHeight * 4f * k * (1f - k));
        
        if(k >= 1f) _state = State.Hovering;
    }
    
    private void UpdateHover()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed + _phase) * bobAmplitude;
        transform.position = _landing + Vector3.up * (hoverHeight + bob);
        visual.localScale = _baseScale;
        TryStartMagnet();
    }
    
    private void TryStartMagnet()
    {
        if(Time.time < _armedAt) return;

        var player = FindPlayer();
        if(player == null) return;

        Vector3 target = PlayerChest(player);
        if((target - transform.position).sqrMagnitude > magnetRadius * magnetRadius) return;

        var inventory = GetInventory(player);
        if(inventory == null) return;

        if (inventory.GetFreeSlots(Item.Category) <= 0)
        {
            NotifyFull();
            return;
        }

        _speed = startSpeed;
        _state = State.Magnet;
    }
    
    private void UpdateMagnet()
    {
        var player = FindPlayer();
        if (player == null)
        {
            _state = State.Hovering;
            return;
        }

        Vector3 target = PlayerChest(player);
        _speed = Mathf.Min(_speed + acceleration * Time.deltaTime, maxSpeed);
        transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);

        float distance = Vector3.Distance(transform.position, target);
        visual.localScale = _baseScale * Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(pickupRadius, pickupRadius * 4f, distance));
        
        if(distance > pickupRadius) return;
        if(TryCollect(player)) return;
        
        _armedAt = Time.time + 1.5f;
        _state = State.Hovering;
    }
    
    private bool TryCollect(PlayerController player)
    {
        if(_state == State.Collected || Item == null || player == null) return false;

        var inventory = GetInventory(player);
        if(inventory == null) return false;

        if (!inventory.TryAdd(Item))
        {
            NotifyFull();
            return false;
        }
        
        _state = State.Collected;
        if(pickupCue != null) AudioService.Instance?.PlaySfx(pickupCue, transform.position);
        ShowText(InventoryText.Get("PICKUP_GOT", Definition != null ? Definition.itemName : "?"), transform.position + Vector3.up * 0.4f);
        Destroy(gameObject);
        return true;
    }
    
    private void NotifyFull()
    {
        if(Time.time < _nextFullMessage) return;
        _nextFullMessage = Time.time + 3f;
        ShowText(InventoryText.Get("PICKUP_BAG_FULL"), transform.position + Vector3.up * 0.4f);
    }
    
    private void ShowText(string content, Vector3 position)
    {
        if(string.IsNullOrEmpty(floatingTextPrefabId)) return;
        UIManager.Instance?.GetFloatingTextService()?.Create(floatingTextPrefabId, Guid.NewGuid().ToString(), content, position);
    }
    
    private void ApplyVisual()
    {
        var rarity = Definition != null ? Definition.rarity : Rarity.Common;
        var color = InventoryUIUtil.RarityColor(rarity);
        bool hasIcon = Definition != null && Definition.icon != null;

        if (iconRenderer != null)
        {
            iconRenderer.enabled = hasIcon;
            iconRenderer.sprite = hasIcon ? Definition.icon : null;
        }
        if(glowRenderer != null) glowRenderer.color = new Color(color.r, color.g, color.b, glowAlpha);

        if(rarityVfx == null) return;
        var main = rarityVfx.main;
        main.startColor = color;
        rarityVfx.Play();
    }
    
    private static Vector3 PlayerChest(PlayerController player) => player.transform.position + Vector3.up;
    
    private static Inventory GetInventory(PlayerController player)
    {
        var runtime = player.PlayerRuntime;
        var config = runtime != null ? runtime.Config : null;
        return config != null ? config.Inventory : null;
    }
    
    private static PlayerController FindPlayer()
    {
        if(s_player != null) return s_player;
        if(Time.unscaledTime < s_nextSearch) return null;

        s_nextSearch = Time.unscaledTime + 1f;
        s_player = FindFirstObjectByType<PlayerController>();
        return s_player;
    }
}