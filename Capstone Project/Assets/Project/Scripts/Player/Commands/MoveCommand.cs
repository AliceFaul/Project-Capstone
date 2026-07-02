using UnityEngine;

public class MoveCommand : ICommand {
    private PlayerMovement _movement;
    private Vector3 _destination;

    public MoveCommand(PlayerMovement movement, Vector3 destination) {
        _movement = movement;
        _destination = destination;
    }

    public void Execute() {
        if(_movement != null) { 
            _movement.MoveTo(_destination);
        } else { 
            Debug.LogWarning("PlayerMovement is not assigned.");
        }
    }
}
