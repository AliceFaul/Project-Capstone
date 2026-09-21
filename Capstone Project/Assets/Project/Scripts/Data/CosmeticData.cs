using UnityEngine;

[CreateAssetMenu(fileName = "New Cosmetic", menuName = "Cosmetic/Data")]
public class CosmeticData : ScriptableObject
{
    [Header("Info")]
    public string cosmeticId;
    public string cosmeticName;
    public Sprite cosmeticIcon;
    
    [Header("Visual")]
    public GameObject cosmeticPrefab;

    [Header("Price (0 = Not need to buy)")]
    public int priceGold;
    public int priceGem;
    
    [Header("Unlock")]
    public bool unlockedByDefault;
}