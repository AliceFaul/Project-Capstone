using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Roll Ability", menuName = "Ability/Roll Ability")]
public class RollAbility : Ability
{
    [Header("Configuration")]
    [SerializeField] private float rollSpeed = 14f;
    [SerializeField] private float rollDuration = 0.4f;

    private PlayerController _controller;
    private PlayerMovement _movement;
    
    public override void Activate(GameObject parent) { }

    public override IEnumerator ActivateCoroutine(GameObject parent)
    {
        if(parent == null) yield break;
        if(_controller == null) _controller = parent.GetComponent<PlayerController>();
        if(_movement == null) _movement = _controller.Movement;
        
        if(_controller == null)
        {
            Debug.LogError($"[Roll Ability] {parent.name} has no implement PlayerController!");
            yield break;
        }

        if (_movement == null)
        {
            Debug.LogError($"[Roll Ability] {parent.name} has no implement PlayerMovement!");
            yield break;
        }
        
        // Get VFX from object pooling
        if (VFXPool != null)
        {
            var instanceFx = VFXPool.Get();
            instanceFx.transform.position = parent.transform.position;
            instanceFx.transform.rotation = parent.transform.rotation;
            instanceFx.transform.SetParent(parent.transform);
        }
        else
        {
            Debug.LogWarning($"[Roll Ability] Not implement VFX prefab to this ability!");
        }
        
        var rollDirection = parent.transform.forward;
        var elapsed = 0f;

        while (elapsed < rollDuration)
        {
            // TODO: Add roll logic with speed and direction in PlayerMovement script
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}