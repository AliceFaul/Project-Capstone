using UnityEngine;
using UnityEngine.InputSystem;

public class DeathDebug : MonoBehaviour
{
    [SerializeField] private PlayerRuntime player;
    [SerializeField] private DeathController deathController;
    [SerializeField] private PlayerDissolve dissolve;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerRuntime>();
        if (deathController == null) deathController = FindFirstObjectByType<DeathController>();
        if (dissolve == null) dissolve = FindFirstObjectByType<PlayerDissolve>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f6Key.wasPressedThisFrame && player != null) player.Kill();

        if (keyboard.f7Key.wasPressedThisFrame && deathController != null && RunSession.Instance == null)
        {
            // Visible model first: the animation handler only listens to state changes while it is enabled.
            if (dissolve != null) dissolve.RestoreImmediately();
            deathController.Revive();
        }
    }
}