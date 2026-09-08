using UnityEngine;

public class AttackCommand : ICommand<Transform> {
    private readonly PlayerCombat _combat;
    private readonly PlayerMovement _movement; // Help to move the player towards the enemy if out of range

    public AttackCommand(PlayerController controller) {
        _combat = controller.Combat;
        _movement = controller.Movement;
    }
    
    public void Execute(Transform target) {
        if (_combat != null)
        {
            _combat.SetTarget(target);
            _movement.MoveToTarget(target, _combat.AttackRange);
        }
    }
}
