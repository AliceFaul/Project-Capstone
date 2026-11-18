using System;
using UnityEngine;

// The lobby object the player clicks to choose a level (map table / gate). Raises OpenRequested for the level select screen.
// Until that screen exists it prints the levels, their difficulties and your combat power to the console.
public class LevelGate : Interactable
{
    [SerializeField] private LevelCatalog catalog;
    [SerializeField] private CombatPowerConfig powerConfig;

    public LevelCatalog Catalog => catalog;
    public CombatPowerConfig PowerConfig => powerConfig;

    public static event Action<LevelGate, PlayerController> OpenRequested;

    protected override void OnInteract(PlayerController player)
    {
        if (catalog == null)
        {
            Debug.LogError("[LevelGate] No LevelCatalog assigned.", this);
            return;
        }

        if (OpenRequested != null) OpenRequested.Invoke(this, player);
        else LogLevels(player);
    }

    private void LogLevels(PlayerController player)
    {
        var data = player.PlayerRuntime != null ? player.PlayerRuntime.Config : null;
        int power = CombatPower.Calculate(data, powerConfig);
        Debug.Log($"[LevelGate] Your combat power: {power}");

        foreach (var level in catalog.Levels)
        {
            if (level == null) continue;

            bool unlocked = LevelUnlock.IsLevelUnlocked(catalog, level, data);
            Debug.Log($"[LevelGate] {level.GetName()} ({level.id}) - {(unlocked ? "unlocked" : "LOCKED")}");

            for (int i = 0; i < level.difficulties.Length; i++)
            {
                var d = level.difficulties[i];
                bool open = LevelUnlock.IsDifficultyUnlocked(level, i, data);
                Debug.Log($"    {d.difficulty}: recommended power {d.recommendedPower}, enemies x{d.enemyHealthMultiplier} HP / x{d.enemyDamageMultiplier} dmg, loot {d.lootDifficulty} {(open ? "" : "(locked)")}");
            }
        }
    }

    // Test without the UI: starts the first level on Normal (select the LevelGate in play mode and use this context menu).
    [ContextMenu("Start first level (Normal)")]
    private async void StartFirstLevelForTest()
    {
        if (catalog == null || catalog.Levels.Count == 0 || catalog.Levels[0] == null) return;
        await LevelStarter.Start(catalog.Levels[0], 0, ignoreLocks: true);
    }
}