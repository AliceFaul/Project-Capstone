using UnityEngine;
using UnityEngine.EventSystems;


// Gắn lên NPC rương (object "ChestNpc"). Click chuột trái vào NPC khi đứng đủ gần thì mở giao diện rương.
// NPC có camera chính có Physics Raycaster, scene có EventSystem.

[RequireComponent(typeof(Collider))]
public class ChestNpcInteractable : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float interactRange = 4f;
    [SerializeField] private Transform player;   // để trống thì tự tìm object có Tag "Player"

    private void Start()
    {
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                player = go.transform;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        GachaPanelController panel = GachaPanelController.Instance;
        if (panel == null)
        {
            Debug.LogWarning("[ChestNpc] Không có GachaPanelController trong scene.");
            return;
        }

        if (panel.IsOpen)
            return;

        if (player != null && Vector3.Distance(player.position, transform.position) > interactRange)
        {
            panel.ShowMessage("Hãy đến gần rương hơn!");
            return;
        }

        panel.OpenPanel();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}