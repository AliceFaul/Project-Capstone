using System;
using UnityEngine;

// Lives on the always-active canvas root: opens/closes the inventory with InputHandler.OnToggleInventory,
// blocks gameplay input while it is open and saves (SaveService) on close if something changed.
public class InventoryScreenController : MonoBehaviour
{
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private InventoryScreenView screenView;
    [Tooltip("Optional. Auto-found in the scene if empty.")]
    [SerializeField] private InputHandler inputHandler;
    [Tooltip("Optional. Auto-found when the screen opens.")]
    [SerializeField] private PlayerController playerController;

    private bool _hooked;
    private float _nextSearch;

    public bool IsOpen => screenRoot != null && screenRoot.activeSelf;

    private void Start()
    {
        screenRoot.SetActive(false);
        TryHook();
    }

    private void Update()
    {
        if (_hooked || Time.unscaledTime < _nextSearch) return;
        _nextSearch = Time.unscaledTime + 1f;
        TryHook();
    }

    private void OnDestroy()
    {
        if (_hooked && inputHandler != null) inputHandler.OnToggleInventory -= Toggle;
    }

    private void TryHook()
    {
        if (inputHandler == null) inputHandler = FindFirstObjectByType<InputHandler>();
        if (inputHandler == null) return;

        inputHandler.OnToggleInventory += Toggle;
        _hooked = true;
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        screenRoot.SetActive(true);
        LockInput(true);
    }

    public void Close()
    {
        screenRoot.SetActive(false);
        LockInput(false);
        SaveIfChanged();
    }

    private void LockInput(bool locked)
    {
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null) return;

        playerController.CmdCombatLocked(locked); // blocks move + attack (PlayerModifier)

        var artifacts = playerController.ArtifactController;
        if (artifacts != null) artifacts.InputLocked = locked; // blocks artifact hotkeys 1/2/3
    }

    private async void SaveIfChanged()
    {
        try
        {
            if (screenView == null || !screenView.HasChanges) return;
            screenView.ClearChanges();

            try
            {
                var startup = StartupProcessor.Instance;
                if (startup == null || !startup.GetService(out SaveService save)) return;
                await save.SaveData(); // local cache first, then PlayFab
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
