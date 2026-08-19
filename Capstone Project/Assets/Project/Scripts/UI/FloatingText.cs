using System;
using UnityEngine;
using TMPro;
using System.Collections;

public enum FloatingTextType
{
    None,
    FloatUp,
    Bounce
}

[RequireComponent(typeof(CanvasGroup))]
public class FloatingText : UIElement
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Animation (Apply when isMoving = true)")]
    [SerializeField] private float moveDistance = 1f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("Static (Apply when isMoving = false)")]
    [Tooltip("Display duration before auto-destruction, for cases where it does not fly upwards.")]
    [SerializeField] private float autoDestructionDuration = 1.5f;
    
    [Tooltip("If Active: Text auto rotate to camera (billboard)")]
    [SerializeField] private bool autoRotateToCamera = true;
    
    private Vector3 _startPosition;
    private Coroutine _routine;
    private Action<string> _onFinished;

    private void Awake()
    {
        if(canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void LateUpdate()
    {
        if (autoRotateToCamera && Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }

    public void Setup(string instanceId, string content, bool isMoving, Action<string> onFinished)
    {
        InstanceID = instanceId;
        text.text = content;
        canvasGroup.alpha = 1f;
        _startPosition = transform.position;
        _onFinished = onFinished;
        
        if(_routine != null)
            StopCoroutine(_routine);
        
        _routine = StartCoroutine(isMoving ? Moving() : AutoDestruction());
    }

    public void StopAndFinish()
    {
        if(_routine != null)
            StopCoroutine(_routine);
        
        _onFinished?.Invoke(InstanceID);
    }

    private IEnumerator Moving()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            
            transform.position = _startPosition + Vector3.up * (moveDistance * moveCurve.Evaluate(normalized));
            canvasGroup.alpha = alphaCurve.Evaluate(normalized);
            yield return null;
        }
        
        _onFinished?.Invoke(InstanceID);
    }

    private IEnumerator AutoDestruction()
    {
        yield return new WaitForSeconds(autoDestructionDuration);
        _onFinished?.Invoke(InstanceID);
    }
}