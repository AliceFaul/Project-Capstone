using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0f, 30f, 0f);

    private void LateUpdate()
    {
        if(player == null) return;
        
        Vector3 position = player.position + offset;
        transform.position = position;

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}