using System;
using System.Collections.Generic;
using Project.Capstone.Inventory;

[Serializable]
public class StatRoll
{
    public BonusStat stat;
    public int tier; // 0..3 (tier 1..4)
}

[Serializable]
public class RollValue
{
    public string id;
    public float value;
}

[Serializable]
public class RuneInstance : IInventoryItem
{
    public string instanceId;
    public string runeId;
    public List<RollValue> rolls = new List<RollValue>();
 
    public string InstanceId => instanceId;
    public InventoryCategory Category => InventoryCategory.Rune;
    // JsonUtility khong luu null cho field class - socket trong se duoc doc ve la object rong, nen dung IsEmpty thay vi == null.
    public bool IsEmpty => string.IsNullOrEmpty(runeId);
}
 
[Serializable]
public class ArtifactInstance : IInventoryItem
{
    public string instanceId;
    public string definitionId;
 
    public string InstanceId => instanceId;
    public InventoryCategory Category => InventoryCategory.Artifact;
}

[Serializable]
public class EquipmentInstance : IInventoryItem
{
    public const int SocketCount = 3;
 
    public string instanceId;
    public string definitionId;
    public InventoryCategory category;
    public int level = 1;
    public float exp;
    public List<StatRoll> rolls = new List<StatRoll>();
    public List<RuneInstance> sockets = new List<RuneInstance>();
 
    public string InstanceId => instanceId;
    public InventoryCategory Category => category;
 
    // Tier da roll cho 1 chi so. Chi so khong co tier (gia tri co dinh) -> 0.
    public int GetTier(BonusStat stat)
    {
        foreach (var roll in rolls)
        {
            if (roll.stat == stat) return roll.tier;
        }
        return 0;
    }
 
    public RuneInstance GetSocket(int index)
    {
        EnsureSockets();
        return index >= 0 && index < sockets.Count ? sockets[index] : null;
    }
 
    public void EnsureSockets()
    {
        if (sockets == null) sockets = new List<RuneInstance>();
        while (sockets.Count < SocketCount) sockets.Add(new RuneInstance());
    }
}