using UnityEngine;

public class ResetAttackCombo : StateMachineBehaviour
{
    [SerializeField] private string triggerName;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger(triggerName);
    }
}
