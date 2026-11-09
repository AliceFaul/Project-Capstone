using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    private static ScreenFader s_instance;
    private CanvasGroup _group;

    public static ScreenFader Instance
    {
        get
        {
            if(s_instance != null) return s_instance;

            var go = new GameObject("ScreenFader");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<ScreenFader>();
            s_instance.Build();
            return s_instance;
        }
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        _group = gameObject.AddComponent<CanvasGroup>();

        var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(transform, false);
        var rect = (RectTransform)image.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.GetComponent<Image>().color = Color.black;
        
        SetAlpha(0f);
    }

    public IEnumerator FadeTo(float target, float duration)
    {
        float start = _group.alpha;
        _group.blocksRaycasts = true;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            _group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        SetAlpha(target);
    }

    public void SetAlpha(float alpha)
    {
        _group.alpha = alpha;
        _group.blocksRaycasts = alpha > 0.01f; // a black screen also swallows clicks
        _group.interactable = false;
    }
}