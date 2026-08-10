using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewAssetReferences", menuName = "Data/AssetReferences")]
public class AssetReferences : ScriptableObject
{
    [SerializeField] private string key;
    [SerializeField] private List<AssetReference> assets;
    [SerializeField] private List<AssetLabelReference> labels;
    
    public string Key => key;
    public List<AssetReference> Assets => assets;
    public List<AssetLabelReference> Labels => labels;
}

[CreateAssetMenu(fileName = "NewAssetReferencesList", menuName = "Data/AssetReferencesList")]
public class AssetReferencesList : ScriptableObject
{
    [SerializeField] private List<AssetReferences> references;
    public List<AssetReferences> References => references;
}
