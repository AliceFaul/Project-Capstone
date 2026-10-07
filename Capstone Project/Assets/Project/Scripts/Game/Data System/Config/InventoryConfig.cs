using UnityEngine;
using System.Collections.Generic;
using Project.Capstone.Inventory;

[CreateAssetMenu(fileName = "Inventory Config", menuName = "Config/Inventory")]
public class InventoryConfig : ScriptableObject
{
    [SerializeField] private List<CategoryCapacity> categories = DefaultCategories();
    public IReadOnlyList<CategoryCapacity> Categories => categories;
 
    public static List<CategoryCapacity> DefaultCategories()
    {
        return new List<CategoryCapacity>
        {
            new CategoryCapacity { category = InventoryCategory.Melee, slotCount = 25 },
            new CategoryCapacity { category = InventoryCategory.Ranged, slotCount = 25 },
            new CategoryCapacity { category = InventoryCategory.Armor, slotCount = 25 },
            new CategoryCapacity { category = InventoryCategory.Rune, slotCount = 25 },
            new CategoryCapacity { category = InventoryCategory.Artifact, slotCount = 25 },
        };
    }
}