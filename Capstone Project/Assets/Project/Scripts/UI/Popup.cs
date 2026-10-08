using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class Popup : UIElement
{
    [SerializeField] private LocalizationText contentText;
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void Setup(string instanceId, LocalizedString content, Func<bool> onClick1, Func<bool> onClick2)
    {
        this.InstanceID = instanceId;
        if(contentText != null) contentText.ChangeText(content);

        if (this.button1 != null)
        {
            this.button1.onClick.RemoveAllListeners();
            this.button1.onClick.AddListener(() =>
            {
                bool close = onClick1 == null || onClick1.Invoke();
                if(close) StartCoroutine(ClosePopup(_animator.GetCurrentAnimatorStateInfo(0).length));
            });
        }
        
        if (this.button2 != null)
        {
            this.button2.onClick.RemoveAllListeners();
            this.button2.onClick.AddListener(() =>
            {
                bool close = onClick2 == null || onClick2.Invoke();
                if(close) StartCoroutine(ClosePopup(_animator.GetCurrentAnimatorStateInfo(0).length));
            });
        }
    }

    public void Setup(string instanceId, LocalizedString content, Action onClick1, Action onClick2)
    {
        Setup(
            instanceId,
            content,
            onClick1 != null ? () => { onClick1(); return true; } : null,
            onClick2 != null ? () => { onClick2(); return true; } : null);
    }
    
    public void Setup(string instanceId, LocalizedString content)
        => Setup(instanceId, content, null, null);
    
    private IEnumerator ClosePopup(float time)
    {
        if(_animator != null) _animator.Play($"Close");
        yield return new WaitForSeconds(time);
        Destroy(this.gameObject);
    }
}