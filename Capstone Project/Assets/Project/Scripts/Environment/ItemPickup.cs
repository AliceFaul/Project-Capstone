using Project.Capstone.Inventory;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Cấu hình vật phẩm rơi")]
    [SerializeField] private ItemData itemData;
    [Min(1)] [SerializeField] private int amount = 1;
    [SerializeField] private EquipmentProgressConfig config;

    private bool _pickedUp;

    private void OnTriggerStay(Collider other)
    {
        if(_pickedUp) return;

        var runtime = other.GetComponentInParent<PlayerController>().PlayerRuntime;
        if(runtime == null) return;

        var playerData = runtime.Config;
        if(playerData == null) return;
        
        var inventory = playerData.Inventory;
        if(inventory == null) return;
        
        if(!TryCreateItem(out var item)) return;

        if (!inventory.TryAdd(item))
        {
            Debug.Log($"[ItemPickup] Inventory is full.");
            return;
        }
        
        _pickedUp = true;
        Debug.Log($"[ItemPickup] Picked up item: {itemData.itemName}");
        Destroy(gameObject);
    }

    private bool TryCreateItem(out IInventoryItem item)
    {
        item = null;

        if (itemData == null)
        {
            Debug.LogError($"[ItemPickup] ItemData is null.");
            return false;
        }

        if (itemData is EquipmentData equipmentData)
        {
            item = LootRoller.RollEquipment(equipmentData, config);
            return item != null;
        }
        
        Debug.LogError($"[ItemPickup] Unsupported item type: '{itemData.GetType().Name}'.");
        return false;
    }
}