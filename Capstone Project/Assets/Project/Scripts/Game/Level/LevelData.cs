using System;
using UnityEngine;
using UnityEngine.Localization;

public enum LevelDifficulty { Normal = 0, Hard = 1, Nightmare = 2 }

[Serializable]
public class LevelDifficultySettings
{
    public LevelDifficulty difficulty;
    [Tooltip("Recommended minimum combat power to clear the level comfortably (shown in the level select screen).")]
    [Min(0)] public int recommendedPower = 100;
    [Min(0.1f)] public float enemyHealthMultiplier = 1f;
    [Min(0.1f)] public float enemyDamageMultiplier = 1f;
    [Tooltip("Becomes LootService.Difficulty: higher = better odds of rare items.")]
    [Min(0)] public int lootDifficulty;
    [Tooltip("Items listed as 'rewards' for this difficulty in the level select screen.")]
    public LootTable rewardPreview;
}

// One playable level (a gameplay scene with its own node map). The level select screen is built from these.
[CreateAssetMenu(fileName = "Level_01", menuName = "Level/Level Data")]
public class LevelData : ScriptableObject
{
    public string id = "level_01";

    [Header("Scene")]
    public string sceneName;
    [Tooltip("Load through the Loading scene + SceneRef preload groups (SceneLoader's initHelper). Match how this scene is loaded today.")]
    public bool preloadWithLoadingScene = true;

    [Header("Display")]
    public LocalizedString displayName;
    public LocalizedString description;
    [Tooltip("Shown until the localized strings above are filled in.")]
    public string fallbackName = "Level";
    [TextArea] public string fallbackDescription;
    public Sprite preview;
    [Tooltip("Where the level marker sits on the map area of the level select screen (0..1 of that area).")]
    public Vector2 mapPosition = new Vector2(0.4f, 0.55f);

    [Header("Difficulties (Normal, Hard, Nightmare)")]
    public LevelDifficultySettings[] difficulties =
    {
        new LevelDifficultySettings { difficulty = LevelDifficulty.Normal, recommendedPower = 100, enemyHealthMultiplier = 1f, enemyDamageMultiplier = 1f, lootDifficulty = 0 },
        new LevelDifficultySettings { difficulty = LevelDifficulty.Hard, recommendedPower = 220, enemyHealthMultiplier = 1.5f, enemyDamageMultiplier = 1.25f, lootDifficulty = 2 },
        new LevelDifficultySettings { difficulty = LevelDifficulty.Nightmare, recommendedPower = 400, enemyHealthMultiplier = 2.2f, enemyDamageMultiplier = 1.6f, lootDifficulty = 4 },
    };

    public LevelDifficultySettings GetDifficulty(int index)
    {
        if (difficulties == null || difficulties.Length == 0) return null;
        return difficulties[Mathf.Clamp(index, 0, difficulties.Length - 1)];
    }

    public string GetName() => ResolveText(displayName, string.IsNullOrEmpty(fallbackName) ? name : fallbackName);
    public string GetDescription() => ResolveText(description, fallbackDescription);

    private static string ResolveText(LocalizedString text, string fallback)
    {
        if (text is { IsEmpty: false })
        {
            string localized = text.GetLocalizedString();
            if (!string.IsNullOrEmpty(localized)) return localized;
        }
        return fallback ?? string.Empty;
    }
}