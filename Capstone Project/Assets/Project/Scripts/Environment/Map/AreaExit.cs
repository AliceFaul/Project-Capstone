using TMPro;
using UnityEngine;

/// Click-to-use exit at the end of a path. Each exit leads to one of the node's next nodes and stays locked until the area is cleared.
public class AreaExit : Interactable
{
    [Header("Presentation")]
    [Tooltip("Optional world-space label showing the type of the next node.")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject openVisual;

    public int TargetNodeId { get; private set; } = -1;
    public NodeType TargetType { get; private set; }
    public bool Locked { get; private set; } = true;

    private Transform _camera;

    public void Bind(NodeType targetType, int targetNodeId, bool locked)
    {
        gameObject.SetActive(true);
        TargetType = targetType;
        TargetNodeId = targetNodeId;
        if (label != null) label.text = InventoryText.Get("NODE_" + targetType.ToString().ToUpper());
        SetLocked(locked);
    }

    public void SetLocked(bool locked)
    {
        Locked = locked;
        if (lockedVisual != null) lockedVisual.SetActive(locked);
        if (openVisual != null) openVisual.SetActive(!locked);
    }

    public override bool CanInteract(PlayerController player)
    {
        var run = LevelRunController.Instance;
        return !Locked && TargetNodeId >= 0 && run != null && !run.IsTransitioning && base.CanInteract(player);
    }

    protected override void OnInteract(PlayerController player) => LevelRunController.Instance.Choose(TargetNodeId);

    private void LateUpdate()
    {
        if (label == null) return;
        if (_camera == null && Camera.main != null) _camera = Camera.main.transform;
        if (_camera != null) label.transform.rotation = _camera.rotation;
    }
}