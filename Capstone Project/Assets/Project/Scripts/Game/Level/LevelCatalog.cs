using System.Collections.Generic;
using UnityEngine;

// All levels in the order they unlock (level N+1 unlocks when level N has been cleared on any difficulty).
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Level/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    [SerializeField] private List<LevelData> levels = new List<LevelData>();

    public IReadOnlyList<LevelData> Levels => levels;

    public int IndexOf(LevelData level) => levels.IndexOf(level);

    public LevelData GetById(string id) => levels.Find(l => l != null && l.id == id);
}