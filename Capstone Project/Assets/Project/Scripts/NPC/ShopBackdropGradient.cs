using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ShopBackdropGradient : MonoBehaviour
{
    [SerializeField] private Color color = new Color(0.02f, 0.06f, 0.09f, 1f);
    [Range(0f, 1f)] [SerializeField] private float maxAlpha = 0.8f;
    
    [Tooltip("Screen width fraction where the gradient starts to fade.")]
    [Range(0f, 1f)] [SerializeField] private float fadeStart = 0.45f;
    
    [Tooltip("Screen width fraction where it is fully transparent.")]
    [Range(0f, 1f)] [SerializeField] private float fadeEnd = 0.85f;

    private Texture2D _texture;

    private void Awake()
    {
        const int width = 128;
        _texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int x = 0; x < width; x++)
        {
            float u = x / (width - 1f);
            float t = Mathf.Clamp01((u - fadeStart) / Mathf.Max(0.0001f, fadeEnd - fadeStart));
            float fade = t * t * (3f - 2f * t);
            _texture.SetPixel(x, 0, new Color(color.r, color.g, color.b, maxAlpha * (1f - fade)));
        }
        _texture.Apply();

        GetComponent<RawImage>().texture = _texture;
    }

    private void OnDestroy()
    {
        if (_texture != null) Destroy(_texture);
    }
}