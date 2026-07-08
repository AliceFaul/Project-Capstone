using UnityEngine;

[CreateAssetMenu(fileName = "New Artifact", menuName = "Inventory/Artifact")]
public class ArtifactData : EquipmentData
{
    public float cooldown;
    public int energyCost;
}