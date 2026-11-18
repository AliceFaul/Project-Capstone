// What the player picked in the lobby: carried across the scene load and read by LevelRunController in the gameplay scene.
// Also what a "Retry" button needs to start the same run again.
public static class RunSettings
{
    public static LevelData Level { get; private set; }
    public static int DifficultyIndex { get; private set; }
    public static int Seed { get; private set; } // 0 = random map every run

    public static bool IsSet => Level != null;
    public static LevelDifficultySettings CurrentDifficulty => Level != null ? Level.GetDifficulty(DifficultyIndex) : null;

    public static void Set(LevelData level, int difficultyIndex, int seed = 0)
    {
        Level = level;
        DifficultyIndex = difficultyIndex;
        Seed = seed;
    }

    public static void Clear()
    {
        Level = null;
        DifficultyIndex = 0;
        Seed = 0;
    }
}