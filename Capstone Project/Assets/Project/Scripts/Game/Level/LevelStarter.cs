using System.Threading.Tasks;
using UnityEngine;

// Starts a run from the lobby: remembers the choice (RunSettings), sets the loot difficulty and loads the level scene through SceneLoader.
// A "Retry" button can call Start(RunSettings.Level, RunSettings.DifficultyIndex).
public static class LevelStarter
{
    public static async Task<bool> Start(LevelData level, int difficultyIndex, PlayerDataConfig data = null,
        LevelCatalog catalog = null, bool ignoreLocks = false)
    {
        if (level == null || string.IsNullOrEmpty(level.sceneName))
        {
            Debug.LogError("[LevelStarter] The level has no scene name.");
            return false;
        }

        if (!ignoreLocks && data != null)
        {
            if (!LevelUnlock.IsLevelUnlocked(catalog, level, data))
            {
                Debug.LogWarning($"[LevelStarter] '{level.id}' is still locked.");
                return false;
            }
            if (!LevelUnlock.IsDifficultyUnlocked(level, difficultyIndex, data))
            {
                Debug.LogWarning($"[LevelStarter] Difficulty {difficultyIndex} of '{level.id}' is still locked.");
                return false;
            }
        }

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[LevelStarter] SceneLoader.Instance is missing.");
            return false;
        }

        RunSettings.Set(level, difficultyIndex);
        var settings = level.GetDifficulty(difficultyIndex);
        LootService.Difficulty = settings != null ? settings.lootDifficulty : 0;

        await SceneLoader.Instance.LoadScene(level.sceneName, level.preloadWithLoadingScene);
        return true;
    }
}