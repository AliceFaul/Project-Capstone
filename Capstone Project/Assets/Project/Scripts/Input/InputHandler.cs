using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour {
    private InputSystem_Actions _actions;

    public Vector2 MousePosition => _actions.Player.MousePosition.ReadValue<Vector2>();

    // Events for input actions
    public event Action OnLeftClick;
    public event Action OnRightClick;
    // TODO: Add more events for other input actions as needed

    private void Awake() {
        _actions = new InputSystem_Actions();
    }

    private void OnEnable() {
        _actions.Enable();

        _actions.Player.LeftClick.performed += LeftClickPerformed;
        _actions.Player.RightClick.performed += RightClickPerformed;
    }

    private void OnDisable() {
        _actions.Disable();

        _actions.Player.LeftClick.performed -= LeftClickPerformed;
        _actions.Player.RightClick.performed -= RightClickPerformed;
    }

    private void LeftClickPerformed(InputAction.CallbackContext context) { 
        OnLeftClick?.Invoke();
    }

    private void RightClickPerformed(InputAction.CallbackContext context)
    {
        OnRightClick?.Invoke();
    }
}
