using UnityEngine;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour
{
    public int slotIndex;

    public void OnClick()
    {
        PlayerInventory.Instance.UseItem(slotIndex);
    }
} 