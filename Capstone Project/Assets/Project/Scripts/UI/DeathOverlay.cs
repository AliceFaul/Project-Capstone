using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathOverlay : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private string headline = "YOU DIED";
    [Tooltip("{0} = lives left.")]
    [SerializeField] private string livesLeftFormat = "{0} lives left";
    [SerializeField] private string gameOverHeadline = "OUT OF LIVES";
    [Tooltip("Empty = the TMP default font. Use the font of your other UI if the texts above contain Vietnamese letters.")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float headlineSize = 64f;
    [SerializeField] private float livesTextSize = 36f;

    [Header("Life icons")]
    [Tooltip("Empty = plain coloured squares. Reuse the heart sprite of your HUD here.")]
    [SerializeField] private Sprite lifeSprite;
    [Tooltip("Optional sprite for a lost life. Empty = the life sprite, dimmed.")]
    [SerializeField] private Sprite lostSprite;
    [Tooltip("Tint of a life icon. Use white if the sprite is already coloured.")]
    [SerializeField] private Color lifeColor = new Color(0.9f, 0.2f, 0.25f, 1f);
    [SerializeField] private Color lostColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private float iconSize = 72f;
    [SerializeField] private float iconSpacing = 24f;

    [Header("Timing and layer")]
    [Min(0f)] [SerializeField] private float fadeDuration = 0.25f;
    [Min(0.1f)] [SerializeField] private float lostAnimationDuration = 0.5f;
    [Tooltip("Must be above ScreenFader (200) or the overlay would be hidden behind the black screen.")]
    [SerializeField] private int sortingOrder = 210;

    // Pause before the lost life animates, so the player first sees the full row.
    private const float ShowBeforeLoss = 0.3f;

    private Canvas _canvas;
    private CanvasGroup _group;
    private TMP_Text _headlineText;
    private TMP_Text _livesText;
    private RectTransform _iconRow;
    private readonly List<Image> _icons = new List<Image>();
    private bool _built;

    // Death with lives left. The row shows the lives BEFORE the death, then the last one is lost.
    // Takes totalSeconds in all (fade in, pop, hold, fade out), never less than the minimum needed to read it.
    public IEnumerator Present(int livesLeft, int maxLives, float totalSeconds)
    {
        EnsureBuilt();
        PrepareRow(maxLives, livesLeft + 1);

        _headlineText.text = headline;
        _livesText.text = string.Format(livesLeftFormat, livesLeft);

        float minimum = fadeDuration * 2f + ShowBeforeLoss + lostAnimationDuration + 0.3f;
        float total = Mathf.Max(totalSeconds, minimum);

        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        yield return new WaitForSecondsRealtime(ShowBeforeLoss);
        yield return StartCoroutine(LoseIcon(livesLeft));

        float used = fadeDuration * 2f + ShowBeforeLoss + lostAnimationDuration;
        if (total > used) yield return new WaitForSecondsRealtime(total - used);

        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
    }

    // Last life lost. The screen STAYS visible (the run summary hides it with Hide()).
    public IEnumerator PresentGameOver(int maxLives)
    {
        EnsureBuilt();
        PrepareRow(maxLives, 1);

        _headlineText.text = gameOverHeadline;
        _livesText.text = string.Empty;

        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        yield return new WaitForSecondsRealtime(ShowBeforeLoss);
        yield return StartCoroutine(LoseIcon(0));
    }

    public IEnumerator Hide()
    {
        if (!_built) yield break;
        yield return StartCoroutine(Fade(_group.alpha, 0f, fadeDuration));
    }

    // Row of 'count' icons where the first 'alive' ones are full.
    private void PrepareRow(int count, int alive)
    {
        while (_icons.Count < count) _icons.Add(CreateIcon());

        for (int i = 0; i < _icons.Count; i++)
        {
            bool used = i < count;
            _icons[i].gameObject.SetActive(used);
            if (used) SetIconState(_icons[i], i < alive);
        }
    }

    private IEnumerator LoseIcon(int index)
    {
        if (index < 0 || index >= _icons.Count) yield break;

        Image icon = _icons[index];
        bool flipped = false;

        for (float t = 0f; t < lostAnimationDuration; t += Time.unscaledDeltaTime)
        {
            float progress = t / lostAnimationDuration;
            icon.transform.localScale = Vector3.one * (1f + 0.5f * Mathf.Sin(progress * Mathf.PI)); // a quick pop

            if (!flipped && progress >= 0.5f)
            {
                flipped = true;
                SetIconState(icon, false);
                icon.transform.localScale = Vector3.one * 1.5f;
            }

            yield return null;
        }

        SetIconState(icon, false);
    }

    private void SetIconState(Image icon, bool alive)
    {
        icon.transform.localScale = Vector3.one;

        if (lifeSprite != null) icon.sprite = alive ? lifeSprite : (lostSprite != null ? lostSprite : lifeSprite);
        icon.color = alive ? lifeColor : lostColor;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        _canvas.enabled = true;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            _group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        _group.alpha = to;
        if (to <= 0f) _canvas.enabled = false;
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        // A child object: this component usually sits on a plain scene object, which must not turn into a UI object.
        var root = new GameObject("DeathOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);

        _canvas = root.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _group = root.GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;

        _headlineText = CreateText("Headline", root.transform, headlineSize, new Vector2(0f, 120f));
        _iconRow = CreateIconRow(root.transform);
        _livesText = CreateText("LivesLeft", root.transform, livesTextSize, new Vector2(0f, -110f));

        _canvas.enabled = false;
    }

    private TMP_Text CreateText(string objectName, Transform parent, float size, Vector2 position)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1400f, size * 1.4f);
        rect.anchoredPosition = position;

        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreateIconRow(Transform parent)
    {
        var go = new GameObject("Lives", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800f, iconSize);
        rect.anchoredPosition = Vector2.zero;

        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = iconSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return rect;
    }

    private Image CreateIcon()
    {
        var go = new GameObject("Life", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(_iconRow, false);

        var element = go.GetComponent<LayoutElement>();
        element.preferredWidth = iconSize;
        element.preferredHeight = iconSize;

        var image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
}