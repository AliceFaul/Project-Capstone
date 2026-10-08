using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Roll Ability", menuName = "Ability/Roll Ability")]
public class RollAbility : Ability
{
    [Header("Configuration")]
    [SerializeField] private float rollSpeed = 14f;
    [SerializeField] private float rollDuration = 0.4f;

    public override void Activate(GameObject parent) { }

    public override IEnumerator ActivateCoroutine(GameObject parent)
    {
        if(parent == null) yield break;
        
        var controller = parent.GetComponent<PlayerController>();
        if(controller == null || controller.Movement == null || controller.StateMachine == null) yield break;

        controller.CmdCombatLocked(true);
        controller.StateMachine.ChangeState(CharacterStateType.Roll);

        var animHandler = controller.AnimationHandler;
        float duration = animHandler != null ? animHandler.GetAnimationLength("Roll") : rollDuration;
        
        // Get VFX from object pooling
        GameObject instanceFx = null;
        if (VFXPool != null)
        {
            instanceFx = VFXPool.Get();
            instanceFx.transform.position = parent.transform.position;
            instanceFx.transform.rotation = parent.transform.rotation;
            instanceFx.transform.SetParent(parent.transform);
        }
        else
        {
            Debug.LogWarning($"[Roll Ability] Not implement VFX prefab to this ability!");
        }
        
        var rollDirection = parent.transform.forward;
        yield return controller.Movement.PerformRoll(rollDirection, rollSpeed, duration);

        controller.CmdCombatLocked(false);

        if (controller.StateMachine != null && controller.StateMachine.IsCurrentState(CharacterStateType.Roll))
        {
            controller.StateMachine.ChangeState(CharacterStateType.Locomotion);
        }
        
        if (instanceFx != null && VFXPool != null)
        {
            instanceFx.transform.SetParent(null);
            VFXPool.Release(instanceFx);
        }
    }
}