using System;
using UnityEngine;
using TMPro;
using System.Collections;
using Random = UnityEngine.Random;

public enum FloatingTextType
{
    Static,
    FloatUp,
    Bounce
}

[RequireComponent(typeof(CanvasGroup))]
public class FloatingText : UIElement
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")] 
    [SerializeField] private FloatingTextType type;
    
    [Header("Float Up")]
    [SerializeField] private float moveDistance = 1f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Bounce")]
    [SerializeField] private float bounceHeight = 0.65f;
    [SerializeField] private float bounceHorizontalDistance = 0.18f;
    [SerializeField] private float bounceRotation = 8f;
    [SerializeField] private AnimationCurve bounceVerticalCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve bounceHorizontalCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve bounceRotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Fade")]
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("Static (Apply when isMoving = false)")]
    [Tooltip("Display duration before auto-destruction, for cases where it does not fly upwards.")]
    [SerializeField] private float autoDestructionDuration = 1.5f;
    
    [Tooltip("If Active: Text auto rotate to camera (billboard)")]
    [SerializeField] private bool autoRotateToCamera = true;
    
    private Vector3 _startPosition;
    private Quaternion _animationRotation = Quaternion.identity;
    private float _horizontalDirection;
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
            Billboard();
        }
    }

    public void Setup(string instanceId, string content, Action<string> onFinished)
    {
        InstanceID = instanceId;
        text.text = content;
        canvasGroup.alpha = 1f;
        _startPosition = transform.position;
        _animationRotation = Quaternion.identity;
        _horizontalDirection = Random.value < 0.5f ? -1 : 1;
        _onFinished = onFinished;
        
        if(_routine != null)
            StopCoroutine(_routine);
        
        _routine = StartCoroutine(PlayAnimation());
    }

    public void StopAndFinish()
    {
        if(_routine != null)
            StopCoroutine(_routine);
        
        _onFinished?.Invoke(InstanceID);
    }

    private IEnumerator PlayAnimation()
    {
        switch (type)
        {
            case FloatingTextType.FloatUp:
                yield return Moving();
                break;
            case FloatingTextType.Bounce:
                yield return Bounce();
                break;
            default: // Static
                yield return AutoDestruction();
                break;
        }
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

    private IEnumerator Bounce()
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            
            float vertical = bounceHeight * bounceVerticalCurve.Evaluate(normalized);
            float horizontal = bounceHorizontalDistance * bounceHorizontalCurve.Evaluate(normalized) * _horizontalDirection;
            float rotation = bounceRotation * bounceRotationCurve.Evaluate(normalized);

            transform.position = _startPosition + Vector3.up * vertical + transform.right * horizontal;
            _animationRotation = Quaternion.Euler(0f, 0f, rotation);
            
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

    private void Billboard()
    {
        if (Camera.main != null)
        {
            Transform cameraTransform = Camera.main.transform;
            Quaternion billboardRotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
        
            transform.rotation = billboardRotation * _animationRotation;
        }
    }
}