using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FillShader : MonoBehaviour
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int SpriteUVMinMaxId = Shader.PropertyToID("_SpriteUVMinMax");
    
    [Header("Material")]
    [SerializeField] private Material material;

    private Image _image;
    private Material _runtimeMaterial;
    private Sprite _cachedSprite;
    
    private void Awake()
    {
        _image = GetComponent<Image>();

        if (material == null)
        {
            Debug.LogError($"{nameof(FillShader)} on {name} has no material assigned!", this);
            enabled = false;
            return;
        }
        
        _runtimeMaterial = new Material(material)
        {
            name = $"{material.name} (Runtime)"
        };
        
        _image.material = _runtimeMaterial;
        
        UpdateFill();
        UpdateSpriteUV();
    }

    private void LateUpdate()
    {
        UpdateFill();
        if(_image.sprite != _cachedSprite) UpdateSpriteUV();
    }

    private void OnDestroy()
    {
        if(_runtimeMaterial != null) Destroy(_runtimeMaterial);
    }

    private void UpdateFill()
    {
        if(_runtimeMaterial == null) return;
        _runtimeMaterial.SetFloat(FillAmountId, _image.fillAmount);
    }

    private void UpdateSpriteUV()
    {
        Sprite sprite = _image.sprite;
        if(sprite == null) return;
        
        Texture tex = sprite.texture;
        if(tex == null) return;
        
        Rect rect = sprite.textureRect;

        Vector4 uvMinMax = new Vector4(
            rect.xMin / tex.width,
            rect.yMin / tex.height,
            rect.xMax / tex.width,
            rect.yMax / tex.height);
        
        _runtimeMaterial.SetVector(SpriteUVMinMaxId, uvMinMax);
        _cachedSprite = sprite;
    }
}