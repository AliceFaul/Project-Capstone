using System;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class LoadingScreen : MonoBehaviour, ILoading
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject rootProgress;
    [SerializeField] private Image progressBar;
    [SerializeField] private LocalizationText messageText;
    [SerializeField] private float fadeDuration = 0.25f;
    
    private Coroutine _fadeRoutine;
    private Coroutine _progressRoutine;

    private void Awake()
    {
        if(canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if(messageText != null)
            messageText.InitText();
        else
            Debug.LogError($"[ILoading] Localization text missing!");
    }

    public async Task Show()
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

    public async Task Hide()
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

    public async Task ShowProgressBar()
    {
        if(rootProgress == null)
            return;
        
        rootProgress.SetActive(true);
        var anim = rootProgress.GetComponent<Animator>();

        if (anim != null)
        {
            anim.Play("Open");
            await Task.Yield();
            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            int milliSec = Mathf.RoundToInt(stateInfo.length * 1000f);
            await Task.Delay(milliSec);
        }
    }

    public async Task HideProgressBar()
    {
        if(rootProgress == null)
            return;
        
        var anim = rootProgress.GetComponent<Animator>();
        if (anim != null && rootProgress.activeInHierarchy)
        {
            anim.Play("Close");
            await Task.Yield();
            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            int milliSec = Mathf.RoundToInt(stateInfo.length * 1000f);
            await Task.Delay(milliSec);
        }
        
        rootProgress.SetActive(false);
    }

    public void SetProgress(float progress01, LocalizedString message = null)
    {
        if(messageText != null && message != null)
            messageText.ChangeText(message);

        if (progressBar != null)
        {
            if(_progressRoutine != null) StopCoroutine(_progressRoutine);
            _progressRoutine = StartCoroutine(SmoothProgress(Mathf.Clamp01(progress01)));
        }
    }

    private IEnumerator SmoothProgress(float target)
    {
        while (Mathf.Abs(progressBar.fillAmount - target) > 0.001f)
        {
            progressBar.fillAmount = Mathf.MoveTowards(progressBar.fillAmount, target, Time.deltaTime * 3f);
            yield return null;
        }
        progressBar.fillAmount = target;
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