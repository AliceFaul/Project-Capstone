using UnityEngine;

public class AttackCommand : ICommand {
    private PlayerCombat _combat;
    private PlayerMovement _movement; // Help to move the player towards the enemy if out of range
    private Transform _target;

    public AttackCommand(PlayerCombat combat, PlayerMovement movement, Transform target) {
        _combat = combat;
        _movement = movement;
        _target = target;
    }

    public void Execute() {
        
    }
}
