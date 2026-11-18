using System;
using UnityEngine;

public static class WorldMessage
{
    public static void Show(string prefabId, string text, Vector3 position)
    {
        if (string.IsNullOrEmpty(prefabId))
        {
            Debug.Log($"[Message] {text}");
            return;
        }

        UIManager.Instance?.GetFloatingTextService()?.Create(prefabId, Guid.NewGuid().ToString(), text, position);
    }
}