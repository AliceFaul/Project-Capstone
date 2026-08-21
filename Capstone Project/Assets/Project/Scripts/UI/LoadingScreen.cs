using System;
using UnityEngine;
using System.Threading.Tasks;

[RequireComponent(typeof(CanvasGroup))]
public class LoadingScreen : MonoBehaviour, ILoading
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.25f;
    
    private Coroutine _fadeRoutine;
    
    public async void Show()
    {
        try
        {
            gameObject.SetActive(true);
            
            await FadeOut();
        
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Missing CanvasGroup: {e.Message}");
        }
    }

    public async void Hide()
    {
        try
        {
            await FadeIn();
        
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            
            gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"Missing CanvasGroup: {e.Message}");
        }
    }

    private async Task Fade(float target)
    {
        float start = canvasGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            await Task.Yield();
        }
        canvasGroup.alpha = target;
    }

    private async Task FadeOut()
    {
        await Fade(1);
    }
    
    private async Task FadeIn()
    {
        await Fade(0);
    }
}