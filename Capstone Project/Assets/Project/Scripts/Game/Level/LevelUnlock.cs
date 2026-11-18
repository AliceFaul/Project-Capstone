// Unlock rules of the GDD (2.9): the next level opens after clearing the previous one; a harder difficulty opens after clearing
// the highest difficulty unlocked so far.
public static class LevelUnlock
{
    public static bool IsLevelUnlocked(LevelCatalog catalog, LevelData level, PlayerDataConfig data)
    {
        int index = catalog != null ? catalog.IndexOf(level) : -1;
        if (index <= 0) return true; // the first level (or one outside the catalog) is always open

        var previous = catalog.Levels[index - 1];
        var progress = previous != null && data != null ? data.GetLevelProgress(previous.id) : null;
        return progress is { highestDifficultyCleared: >= 0 };
    }

    public static bool IsDifficultyUnlocked(LevelData level, int difficultyIndex, PlayerDataConfig data)
    {
        if (difficultyIndex <= 0) return true;

        var progress = data != null && level != null ? data.GetLevelProgress(level.id) : null;
        return progress != null && progress.highestDifficultyCleared >= difficultyIndex - 1;
    }

    // Highest difficulty the player may pick right now.
    public static int HighestUnlockedDifficulty(LevelData level, PlayerDataConfig data)
    {
        int count = level != null && level.difficulties != null ? level.difficulties.Length : 1;
        var progress = data != null && level != null ? data.GetLevelProgress(level.id) : null;
        int cleared = progress != null ? progress.highestDifficultyCleared : -1;
        return System.Math.Min(count - 1, cleared + 1);
    }
}