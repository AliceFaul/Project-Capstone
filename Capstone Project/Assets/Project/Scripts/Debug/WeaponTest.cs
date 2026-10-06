using UnityEngine;
using Project.Capstone.Inventory;

public class WeaponTest : MonoBehaviour
{
    [Header("Weapon Definitions")]
    [SerializeField] private WeaponDefinition weaponDefinition;

    [Header("Armor Definition")]
    [SerializeField] private ArmorDefinition armorDefinition;

    private EquipmentManager _equipmentManager;

    private void Start()
    {
        _equipmentManager = EquipmentManager.Instance;

        if (_equipmentManager == null)
        {
            Debug.LogError(
                "[Phase3Test] EquipmentManager.Instance is NULL.");
            return;
        }

        Debug.Log("========================================");
        Debug.Log("       EQUIPMENT PHASE 3 TEST START");
        Debug.Log("========================================");

        _equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;

        ClearEquipment();

        TestWeaponEquip();
        TestWeaponReplace();
        TestWeaponUnequip();

        TestArmor();

        Debug.Log("========================================");
        Debug.Log("        EQUIPMENT PHASE 3 TEST END");
        Debug.Log("========================================");
    }

    private void OnDestroy()
    {
        if (_equipmentManager != null)
        {
            _equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }

    // =========================================================
    // EVENT TEST
    // =========================================================

    private void HandleEquipmentChanged(EquipmentChangedEventArgs args)
    {
        string oldId = args.OldItem != null
            ? args.OldItem.InstanceId
            : "NULL";

        string newId = args.NewItem != null
            ? args.NewItem.InstanceId
            : "NULL";

        Debug.Log(
            $"[Phase3Test][EVENT] " +
            $"Type = {args.EquipmentType} | " +
            $"Old = {oldId} | " +
            $"New = {newId}");
    }

    // =========================================================
    // CLEAN
    // =========================================================

    private void ClearEquipment()
    {
        Debug.Log("========== CLEAR EQUIPMENT ==========");

        _equipmentManager.Unequip(EquipmentType.MeleeWeapon);
        _equipmentManager.Unequip(EquipmentType.RangedWeapon);
        _equipmentManager.Unequip(EquipmentType.Armor);

        LogCurrentEquipment();
    }

    // =========================================================
    // TEST 1
    // =========================================================

    private void TestWeaponEquip()
    {
        Debug.Log("========== TEST 1: EQUIP WEAPON ==========");

        if (weaponDefinition == null)
        {
            Debug.LogError(
                "[Phase3Test] WeaponDefinition is not assigned.");
            return;
        }

        Weapon weaponA =
            new Weapon(weaponDefinition);

        Debug.Log(
            $"Weapon A Created | " +
            $"InstanceId = {weaponA.InstanceId}");

        Debug.Log(
            $"Weapon A Definition = " +
            $"{weaponA.WeaponDefinition.itemName}");

        _equipmentManager.Equip(weaponA);

        Debug.Log(
            $"Current Melee = " +
            $"{GetInstanceId(_equipmentManager.Melee)}");

        bool passed =
            _equipmentManager.Melee != null &&
            _equipmentManager.Melee.InstanceId == weaponA.InstanceId;

        Debug.Log(
            passed
                ? "[PASS] Weapon equipped successfully."
                : "[FAIL] Weapon was not equipped correctly.");
    }

    // =========================================================
    // TEST 2
    // =========================================================

    private void TestWeaponReplace()
    {
        Debug.Log("========== TEST 2: REPLACE WEAPON ==========");

        Weapon weaponA =
            new Weapon(weaponDefinition);

        Weapon weaponB =
            new Weapon(weaponDefinition);

        Debug.Log(
            $"Weapon A ID = {weaponA.InstanceId}");

        Debug.Log(
            $"Weapon B ID = {weaponB.InstanceId}");

        Debug.Log(
            $"Same Definition = " +
            $"{weaponA.WeaponDefinition == weaponB.WeaponDefinition}");

        Debug.Log(
            $"Different Instance ID = " +
            $"{weaponA.InstanceId != weaponB.InstanceId}");

        // Equip A
        _equipmentManager.Equip(weaponA);

        // Replace A with B
        _equipmentManager.Equip(weaponB);

        bool passed =
            _equipmentManager.Melee != null &&
            _equipmentManager.Melee.InstanceId == weaponB.InstanceId;

        Debug.Log(
            passed
                ? "[PASS] Weapon replacement successful."
                : "[FAIL] Weapon replacement failed.");

        Debug.Log(
            $"Current Melee InstanceId = " +
            $"{GetInstanceId(_equipmentManager.Melee)}");
    }

    // =========================================================
    // TEST 3
    // =========================================================

    private void TestWeaponUnequip()
    {
        Debug.Log("========== TEST 3: UNEQUIP WEAPON ==========");

        Weapon weapon =
            new Weapon(weaponDefinition);

        _equipmentManager.Equip(weapon);

        Debug.Log(
            $"Before Unequip = " +
            $"{GetInstanceId(_equipmentManager.Melee)}");

        _equipmentManager.Unequip(weapon);

        bool passed =
            _equipmentManager.Melee == null;

        Debug.Log(
            passed
                ? "[PASS] Weapon unequip successful."
                : "[FAIL] Weapon unequip failed.");

        Debug.Log(
            $"After Unequip = " +
            $"{GetInstanceId(_equipmentManager.Melee)}");
    }

    // =========================================================
    // TEST 4
    // =========================================================

    private void TestArmor()
    {
        Debug.Log("========== TEST 4: ARMOR ==========");

        if (armorDefinition == null)
        {
            Debug.LogWarning(
                "[Phase3Test] ArmorDefinition is not assigned. " +
                "Skipping armor test.");
            return;
        }

        Armor armor =
            new Armor(armorDefinition);

        Debug.Log(
            $"Armor Created | " +
            $"InstanceId = {armor.InstanceId}");

        _equipmentManager.Equip(armor);

        bool passed =
            _equipmentManager.Armor != null &&
            _equipmentManager.Armor.InstanceId == armor.InstanceId;

        Debug.Log(
            passed
                ? "[PASS] Armor equipped successfully."
                : "[FAIL] Armor equip failed.");

        Debug.Log(
            $"Current Armor = " +
            $"{GetInstanceId(_equipmentManager.Armor)}");
    }
    
    private void LogCurrentEquipment()
    {
        Debug.Log(
            $"Melee  = {GetInstanceId(_equipmentManager.Melee)}");

        Debug.Log(
            $"Ranged = {GetInstanceId(_equipmentManager.Ranged)}");

        Debug.Log(
            $"Armor  = {GetInstanceId(_equipmentManager.Armor)}");
    }

    private string GetInstanceId(Item item)
    {
        return item != null
            ? item.InstanceId
            : "NULL";
    }
}