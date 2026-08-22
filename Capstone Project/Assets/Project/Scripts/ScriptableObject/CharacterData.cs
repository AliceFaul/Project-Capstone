using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Data/Character")]
public class CharacterData : ScriptableObject // use for all entity character
{
    public int characterId;
    public GameObject characterPrefab;

    public float expOnKill;
    public float goldOnKill;

    public int level;
    public int baseHealth;
    public int baseDefense;
    public int baseDamage;
    public float baseSpeed;
    public float baseAttackSpeed;
    public float baseAttackRange;
    public float baseCritChance;
    public float baseCritDamage;
}