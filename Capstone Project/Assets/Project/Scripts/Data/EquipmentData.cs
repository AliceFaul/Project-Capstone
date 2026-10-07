using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

public enum EquipmentType { None, MeleeWeapon, RangedWeapon, Armor, Artifact, Special }
public enum WeaponType { Sword, Axe, Bow, Crossbow }

[CreateAssetMenu(menuName = "Inventory/Equipment Data",  fileName = "New Equipment Data")]
public class EquipmentData : ItemData
{
    [Header("Runtime Key")]
    public string equipmentPrefabKey;
    public string EquipmentPrefabKey => equipmentPrefabKey;
 
    [Header("Equipment")]
    public EquipmentType equipmentType;
    public WeaponType weaponType;
 
    [Header("Stats")]
    public List<StatTierTable> stats = new();
 
    [Header("Visuals")]
    public AssetReference equipmentPrefabRef; // just for editor
    public AssetReference EquipmentPrefabRef => equipmentPrefabRef;
 
#if UNITY_EDITOR
    private void OnValidate()
    {
        equipmentPrefabKey = equipmentPrefabRef != null && equipmentPrefabRef.editorAsset != null
            ? equipmentPrefabRef.editorAsset.name
            : null;
    }
#endif
 
    public StatTierTable GetStatTable(BonusStat stat)
    {
        foreach (var table in stats)
        {
            if (table.Stat == stat) return table;
        }
        return null;
    }
}
 
public static class WeaponFactory
{
    public static async Task<GameObject> Create(EquipmentData equipment, Transform socket)
    {
        if (equipment == null)
        {
            Debug.LogError($"[WeaponFactory] Equipment data is null, can't create weapon");
            return null;
        }
 
        if (socket == null)
        {
            Debug.LogError($"[WeaponFactory] Socket (in player hand) is null");
            return null;
        }
 
        GameObject prefab = null;
 
        if (!string.IsNullOrEmpty(equipment.EquipmentPrefabKey) &&
            ResourceManager.Instance != null &&
            ResourceManager.Instance.IsLoaded(equipment.EquipmentPrefabKey))
        {
            prefab = ResourceManager.Instance.GetAsset<GameObject>(equipment.EquipmentPrefabKey);
        }
        else
        {
            Debug.LogError($"[WeaponFactory] {equipment.itemName} haven't preload in ResourceManager, " +
                           $"doing fallback load directly (slow). " +
                           $"Needed call Preload in ResourceManager");
 
#if UNITY_EDITOR
            var tempRef = equipment.EquipmentPrefabRef;
            if (tempRef != null)
            {
                var handle = tempRef.LoadAssetAsync<GameObject>();
                await handle.Task;
 
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    prefab = handle.Result;
                }
            }
#endif
        }
 
        if (prefab == null)
        {
            Debug.LogError($"[WeaponFactory] Not found prefab in '{equipment.itemName}' (key: {equipment.EquipmentPrefabKey}).");
            return null;
        }
 
        GameObject instance = Object.Instantiate(prefab, socket);
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        instance.name = $"{equipment.itemName}_Visual";
        return instance;
    }
 
    public static void DestroyInstance(GameObject instance)
    {
        if (instance == null)
            return;
 
        Object.Destroy(instance);
    }
}