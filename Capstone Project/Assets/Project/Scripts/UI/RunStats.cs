using UnityEngine;

public static class RunStats
{
    public static int EnemiesDefeated { get; private set; }
    public static int GoldEarned { get; private set; }
    public static float ElapsedSeconds => _active ? Time.time - _startTime : 0f;

    private static bool _active;
    private static float _startTime;
    private static int _lastGold;
    private static PlayerDataConfig _data;

    public static void Begin(PlayerDataConfig data)
    {
        Stop();

        _data = data;
        _startTime = Time.time;
        EnemiesDefeated = 0;
        GoldEarned = 0;
        _lastGold = data != null && data.Currency != null ? data.Currency.Gold() : 0;
        _active = true;

        Mhieu.Enemy.EnemyHealth.AnyDied += OnEnemyDied;
        if (data != null && data.Currency != null) data.Currency.OnCurrencyChanged += OnCurrencyChanged;
    }

    public static void Stop()
    {
        if (!_active) return;

        Mhieu.Enemy.EnemyHealth.AnyDied -= OnEnemyDied;
        if (_data != null && _data.Currency != null) _data.Currency.OnCurrencyChanged -= OnCurrencyChanged;
        _data = null;
        _active = false;
    }

    private static void OnEnemyDied(Mhieu.Enemy.EnemyHealth enemy) => EnemiesDefeated++;

    private static void OnCurrencyChanged(CurrencyType type, int total)
    {
        if (type != CurrencyType.Gold) return;

        if (total > _lastGold) GoldEarned += total - _lastGold; // spending lowers the total but is not "earned" back
        _lastGold = total;
    }
}