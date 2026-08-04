using System;
using UnityEngine;

public class ActiveWeapon : MonoBehaviour
{
    [Header("Melee Weapon")]
    [SerializeField] private GameObject meleeVisual;
    [SerializeField] private MeshFilter meleeMeshFilter;
    [SerializeField] private MeshRenderer meleeMeshRenderer;
    
    [Header("Ranged Weapon")]
    [SerializeField] private GameObject rangedVisual;
    [SerializeField] private MeshFilter rangedMeshFilter;
    [SerializeField] private MeshRenderer rangedMeshRenderer;
    
    private EquipmentManager _equipmentManager;

    private void Start()
    {
        _equipmentManager = EquipmentManager.Instance;
        UpdateWeapons();
        
        _equipmentManager.OnEquipmentChanged += UpdateWeapons;
    }

    public void UpdateWeapons()
    {
        if(_equipmentManager == null)
            return;
        
        UpdateMeleeWeapon(_equipmentManager.Melee);
        UpdateRangedWeapon(_equipmentManager.Ranged);
    }

    private void UpdateMeleeWeapon(EquipmentData equipment)
    {
        if(equipment == null)
            return;
        
        if(equipment.equipmentType != EquipmentType.MeleeWeapon)
            return;

        meleeMeshFilter.sharedMesh = equipment.equipmentMesh;
        meleeMeshRenderer.sharedMaterials = equipment.equipmentMaterial;
        Debug.Log($"Change melee weapon: {equipment.itemName}");
    }

    private void UpdateRangedWeapon(EquipmentData equipment)
    {
        if (equipment == null)
            return;
        
        if(equipment.equipmentType != EquipmentType.RangedWeapon)
            return;
        
        rangedMeshFilter.sharedMesh = equipment.equipmentMesh;
        rangedMeshRenderer.sharedMaterials = equipment.equipmentMaterial;
        Debug.Log($"Change ranged weapon: {equipment.itemName}");
    }
    
    // === HELPER SHOW AND HIDE WEAPON
    public void ShowMelee() => meleeVisual.SetActive(true);
    public void ShowRanged() => rangedVisual.SetActive(true);
    public void HideMelee() => meleeVisual.SetActive(false);
    public void HideRanged() => rangedVisual.SetActive(false);
}