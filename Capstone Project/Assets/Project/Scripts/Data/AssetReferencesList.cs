using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAssetReferencesList", menuName = "Data/AssetReferencesList")]
public class AssetReferencesList : ScriptableObject
{
    [SerializeField] private List<AssetReferences> references;
    public List<AssetReferences> References => references;
}