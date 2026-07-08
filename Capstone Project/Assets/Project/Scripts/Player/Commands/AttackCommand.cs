using UnityEngine;

public class AttackCommand : ICommand {
    private readonly PlayerCombat _combat;
    private readonly PlayerMovement _movement; // Help to move the player towards the enemy if out of range
    private readonly Transform _target;

    public AttackCommand(PlayerCombat combat, PlayerMovement movement, Transform target) {
        _combat = combat;
        _movement = movement;
        _target = target;
    }
    
    public void Execute() {
        if (_combat != null)
        {
            _combat.SetTarget(_target);
            _movement.MoveToTarget(_target, _combat.AttackRange);
        }
    }
}
