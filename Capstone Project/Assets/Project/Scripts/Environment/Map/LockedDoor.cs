using UnityEngine;
using UnityEngine.AI;

/// A door that opens with the matching AreaKey of the same area. Closed again every time the area is entered.
/// Put a NavMeshObstacle (Carve) on it so paths are blocked while it is closed.
public class LockedDoor : Interactable, IAreaResettable
{
    [SerializeField] private string keyId = "key";

    [Header("Closed state")]
    [Tooltip("Colliders that block the way while the door is closed (the clickable collider can be one of them).")]
    [SerializeField] private Collider[] blockers;
    [SerializeField] private NavMeshObstacle obstacle;

    [Header("Presentation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;
    [SerializeField] private AudioCue openCue;
    [SerializeField] private AudioCue lockedCue;
    [SerializeField] private string floatingTextPrefabId;

    public bool IsOpen { get; private set; }

    private AreaController _area;

    private void Awake() => _area = GetComponentInParent<AreaController>();

    // Stays clickable while locked so the player gets the "find the key" message.
    public override bool CanInteract(PlayerController player) => !IsOpen && base.CanInteract(player);

    protected override void OnInteract(PlayerController player)
    {
        if (_area == null) _area = GetComponentInParent<AreaController>();

        if (_area != null && _area.HasKey(keyId))
        {
            SetOpen(true);
            if (openCue != null) AudioService.Instance?.PlaySfx(openCue, transform.position);
            if (animator != null && !string.IsNullOrEmpty(openTrigger)) animator.SetTrigger(openTrigger);
            WorldMessage.Show(floatingTextPrefabId, InventoryText.Get("DOOR_OPENED"), transform.position + Vector3.up * 1.5f);
            return;
        }

        if (lockedCue != null) AudioService.Instance?.PlaySfx(lockedCue, transform.position);
        WorldMessage.Show(floatingTextPrefabId, InventoryText.Get("DOOR_LOCKED"), transform.position + Vector3.up * 1.5f);
    }

    public void ResetForEnter()
    {
        SetOpen(false);
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        if (blockers != null)
        {
            foreach (var blocker in blockers)
            {
                if (blocker != null) blocker.enabled = !open;
            }
        }
        if (obstacle != null) obstacle.enabled = !open;
        if (closedVisual != null) closedVisual.SetActive(!open);
        if (openVisual != null) openVisual.SetActive(open);
    }
}