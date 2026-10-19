using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private InputSystem_Actions _actions;

    public Vector2 MousePosition => _actions.Player.MousePosition.ReadValue<Vector2>();

    // Events for input actions
    public event Action OnLeftClick;

    public event Action OnRightClick;

    // TODO: Add more events for other input actions as needed
    public event Action OnArtifact1;
    public event Action OnArtifact2;
    public event Action OnArtifact3;
    public event Action OnToggleInventory;
    public event Action OnToggleMinimap;

    private void Awake()
    {
        _actions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _actions.Enable();

        _actions.Player.LeftClick.performed += LeftClickPerformed;
        _actions.Player.RightClick.performed += RightClickPerformed;
        _actions.Player.Artifact1.performed += OnArtifact1Performed;
        _actions.Player.Artifact2.performed += OnArtifact2Performed;
        _actions.Player.Artifact3.performed += OnArtifact3Performed;
        _actions.Player.ToggleInventory.performed += ToggleInventoryPerformed;
        _actions.Player.ToggleMinimap.performed += ToggleMinimapPerformed;
    }

    private void OnDisable()
    {
        _actions.Disable();

        _actions.Player.LeftClick.performed -= LeftClickPerformed;
        _actions.Player.RightClick.performed -= RightClickPerformed;
        _actions.Player.Artifact1.performed -= OnArtifact1Performed;
        _actions.Player.Artifact2.performed -= OnArtifact2Performed;
        _actions.Player.Artifact3.performed -= OnArtifact3Performed;
        _actions.Player.ToggleInventory.performed -= ToggleInventoryPerformed;
        _actions.Player.ToggleMinimap.performed -= ToggleMinimapPerformed;
    }

    private void LeftClickPerformed(InputAction.CallbackContext context)
    {
        OnLeftClick?.Invoke();
    }

    private void RightClickPerformed(InputAction.CallbackContext context)
    {
        OnRightClick?.Invoke();
    }

    private void OnArtifact1Performed(InputAction.CallbackContext context) => OnArtifact1?.Invoke();
    private void OnArtifact2Performed(InputAction.CallbackContext context) => OnArtifact2?.Invoke();
    private void OnArtifact3Performed(InputAction.CallbackContext context) => OnArtifact3?.Invoke();
    
    private void ToggleInventoryPerformed(InputAction.CallbackContext context)
    {
        OnToggleInventory?.Invoke();
    }

    private void ToggleMinimapPerformed(InputAction.CallbackContext context)
    {
        OnToggleMinimap?.Invoke();
    }
}
