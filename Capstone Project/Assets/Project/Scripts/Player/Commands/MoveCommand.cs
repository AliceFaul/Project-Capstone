using UnityEngine;

public class MoveCommand : ICommand {
    private readonly PlayerMovement _movement;
    private readonly PlayerModifier _modifier;
    private readonly Vector3 _destination;

    public MoveCommand(PlayerController controller, Vector3 destination) {
        _movement = controller.Movement;
        _modifier = controller.PlayerModifier;
        _destination = destination;
    }

    public void Execute() {
        if(_movement != null) {
            if (!_modifier.CanMove)
                return;
            
            _movement.MoveTo(_destination);
        } else { 
            Debug.LogWarning("PlayerMovement is not assigned.");
        }
    }
}
