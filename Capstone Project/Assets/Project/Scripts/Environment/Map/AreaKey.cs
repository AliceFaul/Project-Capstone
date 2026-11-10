using UnityEngine;

/// A key lying in an area. Click to take it: every LockedDoor of the same area with the same key id can then be opened.
/// Keys are per visit of the area (they are cleared when the area is entered), not inventory items.
public class AreaKey : Interactable, IAreaResettable
{
    [SerializeField] private string keyId = "key";
    [SerializeField] private AudioCue pickupCue;
    [SerializeField] private string floatingTextPrefabId;

    private AreaController _area;

    private void Awake() => _area = GetComponentInParent<AreaController>();

    public void ResetForEnter() => gameObject.SetActive(true);

    protected override void OnInteract(PlayerController player)
    {
        if (_area == null) _area = GetComponentInParent<AreaController>();
        if (_area == null)
        {
            Debug.LogError("[AreaKey] Must be inside an AreaController.", this);
            return;
        }

        _area.AddKey(keyId);
        if (pickupCue != null) AudioService.Instance?.PlaySfx(pickupCue, transform.position);
        WorldMessage.Show(floatingTextPrefabId, InventoryText.Get("KEY_FOUND"), transform.position + Vector3.up);
        gameObject.SetActive(false);
    }
}