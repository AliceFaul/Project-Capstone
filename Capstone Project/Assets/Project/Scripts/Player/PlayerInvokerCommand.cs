using UnityEngine;

public class PlayerInvokerCommand : MonoBehaviour {
    // TODO: You can add a queue to store commands if you want to implement undo/redo functionality
    public void ExecuteCommand(ICommand command) { 
        command?.Execute();
    }

    public void ExecuteCommand<T>(ICommand<T> command, T data)
    {
        command?.Execute(data);
    }
}
