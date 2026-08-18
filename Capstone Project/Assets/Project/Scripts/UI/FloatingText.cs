using System;
using UnityEngine;
using TMPro;

public class FloatingText : UIElement
{
    [SerializeField] private TMP_Text text;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        Destroy(gameObject, _animator.GetCurrentAnimatorStateInfo(0).length);
    }

    public void SetText(string content)
    {
        text.text = content;
    }
}