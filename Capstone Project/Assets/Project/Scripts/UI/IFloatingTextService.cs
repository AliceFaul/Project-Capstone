using UnityEngine;
using UnityEngine.Localization;

public interface IFloatingTextService : IUIService
{
    void Create(string prefabId, 
                string instanceId, 
                string content, 
                Vector3 position);
    
    void Create(string prefabId, 
                string instanceId, 
                LocalizedString content, 
                Vector3 position);

    void Create(string prefabId, 
                string instanceId, 
                LocalizedString content, 
                Vector3 position, 
                bool isMoving);

    void Create(string prefabId, 
                string instanceId, 
                string content, 
                Vector3 position, 
                bool isMoving);
}