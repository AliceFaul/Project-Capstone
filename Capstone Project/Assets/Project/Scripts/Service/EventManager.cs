using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

public class EventManager : MonoBehaviour, IManager
{
    public static EventManager Instance { get; private set; }
    private readonly Dictionary<string, Action> _eventListeners = new Dictionary<string, Action>();
    
    public async Task<bool> Initialize()
    {
        await Task.CompletedTask;
        return true;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddListener(string eventName, Action callback)
    {
        _eventListeners.TryAdd(eventName, null);
        _eventListeners[eventName] += callback;
        Debug.Log($"[EventManager] Added event: {eventName}");
    }

    public void RemoveListener(string eventName, Action callback)
    {
        if(_eventListeners.ContainsKey(eventName)) _eventListeners[eventName] -= callback;
        Debug.Log($"[EventManager] Removed event: {eventName}");
    }

    public void Trigger(string eventName)
    {
        if(_eventListeners.TryGetValue(eventName, out var callback)) callback?.Invoke();
        Debug.Log($"[EventManager] Triggered event: {eventName}");
    }
}