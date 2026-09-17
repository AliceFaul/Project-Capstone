using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Cosmetic Database", menuName = "Cosmetic/Database")]
public class CosmeticDatabase : ScriptableObject
{
    public List<CosmeticData> cosmetics;
    private Dictionary<string, CosmeticData> _lookupCosmetic;

    public void Initialize()
    {
        _lookupCosmetic = new Dictionary<string, CosmeticData>();
        foreach (var cosmetic in cosmetics)
        {
            _lookupCosmetic[cosmetic.cosmeticId] = cosmetic;
        }
    }

    public CosmeticData GetCosmetic(string cosmeticId)
    {
        if (_lookupCosmetic == null) Initialize();
        _lookupCosmetic.TryGetValue(cosmeticId, out var result);
        return result;
    }
}