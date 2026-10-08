using UnityEngine;
using System.Collections.Generic;
using System;

public class ServiceRegistry : IServiceRegistry
{
    private readonly Dictionary<Type, object> _services = new();
    
    public void Register<TService>(TService service)
    {
        _services.Add(typeof(TService), service);
    }

    public TService Get<TService>()
    {
        if (_services.TryGetValue(typeof(TService), out var service))
        {
            return (TService)service;
        }
        throw new InvalidOperationException($"No service registered for type {typeof(TService).Name}");
    }

    public bool TryGet<TService>(out TService service)
    {
        if (_services.TryGetValue(typeof(TService), out var s))
        {
            service = (TService)s;
            return true;
        }

        service = default(TService);
        return false;
    }
}