using UnityEngine;

public class MoveCommand : ICommand<Vector3>  {
    private readonly PlayerMovement _movement;
    private readonly PlayerModifier _modifier;

    public MoveCommand(PlayerController controller) {
        _movement = controller.Movement;
        _modifier = controller.PlayerModifier;
    }

    public void Execute(Vector3 destination) {
        if(_movement != null) {
            if (!_modifier.CanMove)
            {
                Debug.Log($"Player can't move because movement is disabled");
                return;
            }
            
            _movement.MoveTo(destination);
        } else { 
            Debug.LogWarning("PlayerMovement is not assigned.");
        }
    }
}
