public interface IServiceRegistry
{
    void Register<TService>(TService service);
    TService Get<TService>();
    bool TryGet<TService>(out TService service);
}