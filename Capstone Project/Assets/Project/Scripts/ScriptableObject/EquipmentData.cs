using UnityEngine;

public enum EquipmentType { None, MeleeWeapon, RangedWeapon, Armor, Artifact, Special }
public enum WeaponType { Sword, Axe, Bow, Crossbow }

[System.Serializable]
public class EquipmentAttribute
{
    public int damage;
    public float attackSpeed;
    public float attackRange;
    public float critChance;
    public float critDamage;
}

[CreateAssetMenu(menuName = "Inventory/Equipment Data",  fileName = "New Equipment Data")]
public class EquipmentData : ItemData
{
    [Header("Equipment")]
    public EquipmentType equipmentType;
    public WeaponType weaponType;
    
    [Header("Level")]
    public int level = 1;
    
    [Header("Attributes")]
    public EquipmentAttribute attributes;
    
    [Header("Sub Stat Modifiers")]
    public int damageModifier;
    public int armorModifier;
    public float speedModifier;
    public float attackSpeedModifier;
    public float attackRangeModifier;
    public float critChanceModifier;
    public float critDamageModifier;
    
    [Header("Visuals")] 
    [SerializeField] private string key;
    public string Key => key;
    
    public Mesh equipmentMesh;
    public Material[] equipmentMaterial;

    public override void Use()
    {
        base.Use();
        EquipmentManager.Instance.Equip(this);
        RemoveFromInventory();
    }
}

public class WeaponFactory
{
    public GameObject Create(string weaponKey, Transform socket)
    {
        GameObject prefab = ResourceManager.Instance.GetAsset<GameObject>(weaponKey);
        if (prefab == null)
        {
            Debug.LogError($"[WeaponFactory] Weapon prefab not found; {weaponKey}");
            return null;
        }
        return Object.Instantiate(prefab, socket);
    }
    
    public void DestroyInstance(GameObject instance)
    {
        if(instance == null)
            return;
        
        Object.Destroy(instance);
    }
}