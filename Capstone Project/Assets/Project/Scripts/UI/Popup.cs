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

    public void Setup(string instanceId, LocalizedString content, Action onClick1, Action onClick2)
    {
        this.InstanceID = instanceId;
        contentText.ChangeText(content);

        if (this.button1 != null)
        {
            this.button1.onClick.AddListener(() =>
            {
                onClick1?.Invoke();
                StartCoroutine(ClosePopup(_animator.GetCurrentAnimatorStateInfo(0).length));
            });
        }
        
        if (this.button2 != null)
        {
            this.button2.onClick.AddListener(() =>
            {
                onClick2?.Invoke();
                StartCoroutine(ClosePopup(_animator.GetCurrentAnimatorStateInfo(0).length));
            });
        }
    }
    
    public void Setup(string instanceId, LocalizedString content)
        => Setup(instanceId, content, null, null);
    
    private IEnumerator ClosePopup(float time)
    {
        _animator.Play($"Close");
        yield return new WaitForSeconds(time);
        Close();
    }
}