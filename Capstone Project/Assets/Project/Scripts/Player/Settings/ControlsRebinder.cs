using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Rebinding for the Controls tab. It works on its own copy of InputSystem_Actions (never enabled); the result is saved as the
// Input System's binding-override JSON in PlayerConfig, and InputHandler loads it into its own copy (GameSettings.ApplyBindings).
public static class ControlsRebinder
{
    private static InputSystem_Actions _actions;
    private static InputActionRebindingExtensions.RebindingOperation _operation;

    public static bool IsRebinding => _operation != null;
    public static int LastEndFrame { get; private set; } = -1; // Esc that cancels a rebind must not also close the screen

    private static InputActionAsset Asset
    {
        get
        {
            if (_actions == null)
            {
                _actions = new InputSystem_Actions();
                GameSettings.ApplyBindings(_actions.asset);
            }
            return _actions.asset;
        }
    }

    private static InputAction GetAction(string actionPath) => Asset.FindAction(actionPath, false);

    // the keyboard / mouse binding of the action (all actions of this project have exactly one)
    private static int BindingIndex(InputAction action)
    {
        if (action == null) return -1;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite) continue;
            if (binding.path.StartsWith("<Keyboard>") || binding.path.StartsWith("<Mouse>")) return i;
        }
        return action.bindings.Count > 0 ? 0 : -1;
    }

    public static string DisplayName(string actionPath)
    {
        var action = GetAction(actionPath);
        int index = BindingIndex(action);
        if (index < 0) return "-";

        string text = InputControlPath.ToHumanReadableString(action.bindings[index].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
        return text.Replace("Left Button", "Left Mouse").Replace("Right Button", "Right Mouse");
    }

    // onDone(true) = a new key was bound, onDone(false) = cancelled (Esc or timeout)
    public static void StartRebind(ControlsConfig config, string actionPath, Action<bool> onDone)
    {
        var action = GetAction(actionPath);
        int index = BindingIndex(action);
        if (index < 0)
        {
            onDone?.Invoke(false);
            return;
        }

        CancelRebind();
        string oldPath = action.bindings[index].effectivePath;

        _operation = action.PerformInteractiveRebinding(index)
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            .WithCancelingThrough("<Keyboard>/escape")
            .WithTimeout(10f)
            .OnComplete(operation =>
            {
                Finish(operation);
                SwapIfInUse(config, action, index, oldPath);
                Persist();
                onDone?.Invoke(true);
            })
            .OnCancel(operation =>
            {
                Finish(operation);
                onDone?.Invoke(false);
            })
            .Start();
    }

    public static void CancelRebind()
    {
        if (_operation == null) return;
        var operation = _operation;
        _operation = null;
        operation.Cancel();
        operation.Dispose();
    }

    public static void ResetActions(ControlsConfig config, ControlSection? section = null)
    {
        foreach (var row in config.rows)
        {
            if (section.HasValue && row.section != section.Value) continue;

            var action = GetAction(row.actionPath);
            int index = BindingIndex(action);
            if (index >= 0) action.RemoveBindingOverride(index);
        }
        Persist();
    }

    private static void Finish(InputActionRebindingExtensions.RebindingOperation operation)
    {
        operation.Dispose();
        _operation = null;
        LastEndFrame = Time.frameCount;
    }

    // If another action already uses the new key, give it the key this action had (the two swap).
    private static void SwapIfInUse(ControlsConfig config, InputAction changed, int changedIndex, string oldPath)
    {
        string newPath = changed.bindings[changedIndex].effectivePath;

        foreach (var row in config.rows)
        {
            var other = GetAction(row.actionPath);
            if (other == null || other == changed) continue;

            int index = BindingIndex(other);
            if (index < 0 || other.bindings[index].effectivePath != newPath) continue;

            other.ApplyBindingOverride(index, oldPath);
        }
    }

    private static void Persist() => GameSettings.SaveBindings(Asset.SaveBindingOverridesAsJson());
}