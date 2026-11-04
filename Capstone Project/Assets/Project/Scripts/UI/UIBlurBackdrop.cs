using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Blurred backdrop for popups on ANY canvas mode (including Screen Space - Overlay).
// Capture() grabs the finished frame at the end of the frame, shrinks and blurs it on the GPU and shows it in a RawImage placed
// just below the dim overlay. It is a snapshot taken when the popup opens, so the cost is paid once, not every frame.
public class UIBlurBackdrop : MonoBehaviour
{
    public enum FlipMode { Auto, Flip, NoFlip }

    private static readonly int FlipId = Shader.PropertyToID("_FlipY");
    private static readonly int DirectionId = Shader.PropertyToID("_Direction");

    [SerializeField] private RawImage blurImage;
    [SerializeField] private Image dimImage;
    [SerializeField] private Shader blurShader;

    [Header("Blur")]
    [Tooltip("Divides the screen resolution before blurring. Higher = softer and cheaper.")]
    [Range(1, 8)] [SerializeField] private int downsample = 4;
    [Range(1, 6)] [SerializeField] private int iterations = 3;
    [Range(0.5f, 3f)] [SerializeField] private float radius = 1.5f;
    [Tooltip("If the blurred picture is upside down, switch between Flip and NoFlip.")]
    [SerializeField] private FlipMode flip = FlipMode.Auto;

    [Header("Dim overlay alpha")]
    [Range(0f, 1f)] [SerializeField] private float dimWithBlur = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float dimWithoutBlur = 0.82f;

    private Material _material;
    private RenderTexture _full;
    private RenderTexture _a;
    private RenderTexture _b;

    public IEnumerator Capture()
    {
        if (!EnsureMaterial())
        {
            Apply(false);
            yield break;
        }

        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;
        if (width < 16 || height < 16)
        {
            Apply(false);
            yield break;
        }

        EnsureTargets(width, height);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(_full);

        bool flipY = flip == FlipMode.Flip || (flip == FlipMode.Auto && SystemInfo.graphicsUVStartsAtTop);
        _material.SetFloat(FlipId, flipY ? 1f : 0f);
        Graphics.Blit(_full, _a, _material, 0);

        for (int i = 0; i < iterations; i++)
        {
            _material.SetVector(DirectionId, new Vector4(radius, 0f, 0f, 0f));
            Graphics.Blit(_a, _b, _material, 1);
            _material.SetVector(DirectionId, new Vector4(0f, radius, 0f, 0f));
            Graphics.Blit(_b, _a, _material, 1);
        }

        blurImage.texture = _a;
        Apply(true);
    }

    // Frees the render textures (call when the popup closes).
    public void Release()
    {
        if (blurImage != null) blurImage.texture = null;
        ReleaseTexture(ref _full);
        ReleaseTexture(ref _a);
        ReleaseTexture(ref _b);
    }

    private void OnDestroy()
    {
        Release();
        if (_material != null) Destroy(_material);
    }

    private void Apply(bool blurred)
    {
        if (blurImage != null) blurImage.enabled = blurred;
        if (dimImage != null)
        {
            var c = dimImage.color;
            dimImage.color = new Color(c.r, c.g, c.b, blurred ? dimWithBlur : dimWithoutBlur);
        }
    }

    private bool EnsureMaterial()
    {
        if (_material != null) return true;

        var shader = blurShader != null ? blurShader : Shader.Find("Hidden/UI/BlurBackdrop");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("[UIBlurBackdrop] Blur shader missing or unsupported - using the plain dim backdrop.");
            return false;
        }

        _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        return blurImage != null;
    }

    private void EnsureTargets(int width, int height)
    {
        int small = Mathf.Max(1, downsample);
        int smallW = Mathf.Max(1, width / small);
        int smallH = Mathf.Max(1, height / small);

        if (_full == null || _full.width != width || _full.height != height)
        {
            ReleaseTexture(ref _full);
            _full = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        }

        if (_a == null || _a.width != smallW || _a.height != smallH)
        {
            ReleaseTexture(ref _a);
            ReleaseTexture(ref _b);
            _a = CreateSmall(smallW, smallH);
            _b = CreateSmall(smallW, smallH);
        }
    }

    private static RenderTexture CreateSmall(int width, int height)
    {
        return new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    private static void ReleaseTexture(ref RenderTexture texture)
    {
        if (texture == null) return;
        texture.Release();
        Object.Destroy(texture);
        texture = null;
    }
}
