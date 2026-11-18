using UnityEngine;

/// <summary>
/// Put on a panel (inventory, pause menu, shop,...) that is ACTIVE only while it is open.
/// </summary>
public class AudioOverlay : MonoBehaviour
{
    [SerializeField, Min(0f)] private float transitionSeconds = 0.25f;

    private void OnEnable() => AudioService.Instance?.SetOverlay(this, true, transitionSeconds);
    private void OnDisable() => AudioService.Instance?.SetOverlay(this, false, transitionSeconds);
}