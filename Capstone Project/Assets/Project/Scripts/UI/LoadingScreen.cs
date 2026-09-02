using System;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class LoadingScreen : MonoBehaviour, ILoading
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image progressBar;
    [SerializeField] private LocalizationText messageText;
    [SerializeField] private float fadeDuration = 0.25f;
    
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if(canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if(messageText != null)
            messageText.InitText();
        else
            Debug.LogError($"[ILoading] Localization text missing!");
    }

    public async void Show()
    {
        try
        {
            gameObject.SetActive(true);
            
            await FadeToVisible();
        
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
            await FadeToHidden();
        
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

    public void ShowProgressBar()
    {
        if(progressBar != null)
            progressBar.gameObject.SetActive(true);
    }

    public void HideProgressBar()
    {
        if(progressBar != null)
            progressBar.gameObject.SetActive(false);
    }

    public void SetProgress(float progress01, LocalizedString message = null)
    {
        if (progressBar != null)
            progressBar.fillAmount = Mathf.Clamp01(progress01);
        
        if(messageText != null && message != null)
            messageText.ChangeText(message);
    }

    public void SetMessage(LocalizedString message)
    {
        if (messageText != null)
            messageText.ChangeText(message);
        else
            Debug.LogError($"[ILoading] TMP_Text missing!");
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

    private async Task FadeToVisible()
    {
        await Fade(1);
    }
    
    private async Task FadeToHidden()
    {
        await Fade(0);
    }
}