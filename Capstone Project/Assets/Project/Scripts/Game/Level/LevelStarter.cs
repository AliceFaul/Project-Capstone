using System.Threading.Tasks;
using UnityEngine;

public static class LevelStarter
{
    private static bool _loading;

    public static async Task<bool> Start(LevelData level, int difficultyIndex, PlayerDataConfig data = null,
        LevelCatalog catalog = null, bool ignoreLocks = false)
    {
        if (_loading) return false;
        if (level == null || string.IsNullOrWhiteSpace(level.sceneName))
        {
            Debug.LogError("[LevelStarter] The level has no scene name.");
            return false;
        }

        var difficulties = level.difficulties;
        if (difficulties == null || difficultyIndex < 0 || difficultyIndex >= difficulties.Length || difficulties[difficultyIndex] == null)
        {
            Debug.LogWarning($"[LevelStarter] '{level.id}' has no settings for difficulty index {difficultyIndex}.", level);
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

        var loader = SceneLoader.Instance;
        if (loader == null)
        {
            Debug.LogError("[LevelStarter] SceneLoader.Instance is missing.");
            return false;
        }

        var previousLevel = RunSettings.Level;
        int previousDifficulty = RunSettings.DifficultyIndex;
        int previousSeed = RunSettings.Seed;
        int previousLoot = LootService.Difficulty;
        _loading = true;
        try
        {
            // The gameplay scene reads these values during startup.
            RunSettings.Set(level, difficultyIndex);
            LootService.Difficulty = difficulties[difficultyIndex].lootDifficulty;
            await loader.LoadScene(level.sceneName, level.preloadWithLoadingScene);
            return true;
        }
        catch
        {
            RunSettings.Set(previousLevel, previousDifficulty, previousSeed);
            LootService.Difficulty = previousLoot;
            throw;
        }
        finally
        {
            _loading = false;
        }
    }
}