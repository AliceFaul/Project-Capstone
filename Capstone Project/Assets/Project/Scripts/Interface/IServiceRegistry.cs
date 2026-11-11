using System;

public interface IServiceRegistry
{
    void Register<TService>(TService service);
    void Register(Type serviceType, object service);
    TService Get<TService>();
    bool TryGet<TService>(out TService service);
}