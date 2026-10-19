using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

/// <summary>
/// List of the OutlineTargets that currently have an outline (hovered, locked or still fading out).
/// This is the bridge between gameplay code and the renderer feature:
/// the feature lives in a renderer asset and cannot reference scene objects directly,
/// so it just reads this list.
/// </summary>
public static class OutlineRegistry
{
    private static readonly List<OutlineTarget> targets = new();
    private static OutlineRunner runner;
    
    /// <summary> Read-only. Do not modify from outside. </summary>
    public static IReadOnlyList<OutlineTarget> Targets => targets;

    // keeps the list clean when "Enter Play Mode Options" skips the domain reload
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        targets.Clear();
        runner = null;
    }

    internal static void Register(OutlineTarget target)
    {
        if(target == null || targets.Contains(target)) return;
        targets.Add(target);
        EnsureRunner();
    }

    internal static void Unregister(OutlineTarget target)
    {
        if(target == null) return;
        targets.Remove(target);
    }

    internal static void Tick(float dt)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            OutlineTarget target = targets[i];
            
        }
    }

    private static void EnsureRunner()
    {
        if(runner != null) return;

        var obj = new GameObject("[OutlineRunner]") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.Object.DontDestroyOnLoad(obj);
        runner = obj.AddComponent<OutlineRunner>();
    }
}

internal sealed class OutlineRunner : MonoBehaviour
{
    private void Update()
    {
        
    }
}