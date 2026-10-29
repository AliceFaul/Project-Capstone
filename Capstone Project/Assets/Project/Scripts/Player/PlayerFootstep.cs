using System;
using UnityEngine;

public class PlayerFootstep : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private FootstepSurfaceMap surfaceMap;
    [SerializeField] private LayerMask groundMask;
    
    [Header("Stride")]
    [Tooltip("Meters walked between two steps.")]
    [SerializeField, Min(0.2f)] private float strideLength = 1.5f;

    [Tooltip("Below this speed (m/s) the character counts as standing still.")]
    [SerializeField, Min(0f)] private float minSpeed = 0.5f;
    
    [Header("Ground / landing")]
    [Tooltip("How far below the feet still counts as ground.")]
    [SerializeField, Min(0.05f)] private float groundCheckDistance = 0.5f;
    
    [Tooltip("Seconds in the air before touching down plays a landing sound.")]
    [SerializeField, Min(0f)] private float minAirTimeForLanding = 0.15f;

    // Moving further than this in one frame is a teleport, not walking.
    private const float MaxFrameDistance = 3f;
    private const float RayStartHeight = 0.3f;

    private Vector3 _lastPosition;
    private float _strideDistance;
    private bool _moving;
    private bool _grounded = true;
    private float _airTime;
    private bool _warnedMissingMap;

    private void Awake()
    {
        if(controller == null) controller = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
        _strideDistance = 0f;
        _moving = false;
        _grounded = true;
        _airTime = 0f;
    }

    private void Update()
    {
        Vector3 position = transform.position;
        Vector3 delta = position - _lastPosition;
        delta.y = 0f;
        _lastPosition = position;
        
        float dt = Time.deltaTime;
        if(dt <= 0f) return; // game paused

        if (!TryGetGround(out RaycastHit hit))
        {
            _grounded = false;
            _airTime += dt;
            _moving = false;
            _strideDistance = 0f;
            return;
        }

        if (!_grounded)
        {
            _grounded = true;
            if(_airTime >= minAirTimeForLanding) PlayLand(hit);
            _airTime = 0f;
        }
        
        float distance = delta.magnitude;
        if (distance > MaxFrameDistance || !CanStep())
        {
            _moving = false;
            _strideDistance = 0f;
            return;
        }

        if (distance / dt < minSpeed)
        {
            _moving = false;
            return;
        }

        if (!_moving)
        {
            _moving = true;
            _strideDistance = Mathf.Max(_strideDistance, strideLength * 0.6f);
        }
        _strideDistance += distance;
        
        if(!(_strideDistance >= strideLength)) return;
        _strideDistance %= strideLength;
        PlayStep(hit);
    }

    private bool CanStep()
    {
        if(controller == null) return true;
        var stateMachine = controller.StateMachine;
        return stateMachine == null || stateMachine.CurrentState == CharacterStateType.Locomotion;
    }

    private bool TryGetGround(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * RayStartHeight;
        return Physics.Raycast(origin, Vector3.down, out hit, RayStartHeight + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void PlayStep(RaycastHit hit)
    {
        var surface = ResolveSurface(hit);
        if(surface == null) return;
        AudioService.Instance?.PlaySfx(surface.step, hit.point);
    }

    private void PlayLand(RaycastHit hit)
    {
        var surface = ResolveSurface(hit);
        if(surface == null) return;

        AudioCue cue = surface.land != null ? surface.land : surface.step;
        AudioService.Instance?.PlaySfx(cue, hit.point);
    }

    private FootstepSurface ResolveSurface(RaycastHit hit)
    {
        if(surfaceMap != null) return surfaceMap.Resolve(hit);
        if(_warnedMissingMap) return null;
        _warnedMissingMap = true;
        Debug.LogWarning("[FootstepPlayer] No FootstepSurfaceMap assigned: footsteps are silent.", this);
        return null;
    }
}