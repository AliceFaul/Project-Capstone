using UnityEngine;
using UnityEngine.Events;

public interface IInteractable
{
    Vector3 InteractPosition { get; }   // where the player faces / walk toward
    float InteractRange { get; }        // flat (XZ) distance at which Interact() may run
    bool CanInteract(PlayerController player);
    void Interact(PlayerController player);
}

/// Base class. Put it on an object with a Collider on the "Interact" layer (PlayerController.interactLayer).
[DisallowMultipleComponent]
public class Interactable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 2.5f;
    
    [Tooltip("Where the player faces / walks to. Empty = this object's position.")]
    [SerializeField] private Transform interactPoint;
    
    [Tooltip("After one successfully interaction CanInteract() becomes false.")]
    [SerializeField] private bool singleUse;
    [SerializeField] private UnityEvent<PlayerController> onInteracted;
    
    public bool Used { get; private set; }
    
    public virtual Vector3 InteractPosition => interactPoint != null ? interactPoint.position : transform.position;
    public virtual float InteractRange => interactRange;
    public virtual bool CanInteract(PlayerController player) => isActiveAndEnabled && !(singleUse && Used);
    
    public void Interact(PlayerController player)
    {
        if(!CanInteract(player)) return;
        if(singleUse) Used = true;
        OnInteract(player);
        onInteracted?.Invoke(player);
    }
    
    public void ResetUse() => Used = false;
    protected virtual void OnInteract(PlayerController player) { }
}