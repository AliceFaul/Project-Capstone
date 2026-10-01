using UnityEngine;

public class CharacterPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform previewPivot;
    [SerializeField] private CosmeticDatabase database;

    private GameObject _currentModel;
    private string _equippedCosmeticId;

    public void Initialize(ICosmetics config)
    {
        if(config == null) return;
        _equippedCosmeticId = config.EquippedCosmeticId;
        RevertCosmetic();
    }
    
    public void PreviewCosmetic(CosmeticData cosmetic)
    {
        if(cosmetic == null || cosmetic.cosmeticPrefab == null) return;
        Clear();

        Transform position = previewPivot != null ? previewPivot : transform;
        _currentModel = Instantiate(cosmetic.cosmeticPrefab, position);
        
        _currentModel.transform.localPosition = Vector3.zero;
        _currentModel.transform.localRotation = Quaternion.identity;
        _currentModel.transform.localScale = Vector3.one;
    }

    public void RevertCosmetic()
    {
        if (database == null)
        {
            Debug.LogError($"[CharacterPreview] Cosmetic Database is MISSING!");
            return;
        }
        
        var equipped = database.GetCosmetic(_equippedCosmeticId);
        if(equipped != null) PreviewCosmetic(equipped);
        else Clear();
    }
    
    public void OnCosmeticEquipped(CosmeticData cosmetic)
    {
        if(cosmetic == null) return;
        _equippedCosmeticId = cosmetic.cosmeticId;
        PreviewCosmetic(cosmetic);
    }
    
    private void Clear()
    {
        if(_currentModel == null) return;
        
        Destroy(_currentModel);
        _currentModel = null;
    }
}