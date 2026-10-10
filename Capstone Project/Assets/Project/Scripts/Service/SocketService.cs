using System;
using Project.Capstone.Inventory;

public enum SocketResult
{
    Success,
    NotWeapon,
    InvalidSocket,
    SocketLocked,
    SocketOccupied,
    SocketEmpty,
    RuneNotInInventory,
    InventoryFull,
    NotEnoughGem,
}

// Manages equipping and unequipping runes for weapon sockets.
// Only weapons (Melee, Ranged) have sockets. Inserting into an unlocked socket is free;
// removing costs gems (level x costPerLevel) and returns the rune back to the inventory.
// uGUI calls this via EquipmentManager.Sockets and reads SocketResult for UI notifications.
public class SocketService
{
    private readonly PlayerDataConfig _data;
    private readonly EquipmentProgressConfig _config;

    public event Action<EquipmentInstance, int> OnSocketChanged;

    public SocketService(PlayerDataConfig data, EquipmentProgressConfig config)
    {
        _data = data;
        _config = config;
    }

    /// <summary>
    /// Calculates the currency cost required to remove a rune from the specified weapon.
    /// Returns 0 if the weapon is null.
    /// </summary>
    public int GetRemovalCost(EquipmentInstance weapon) =>
        weapon == null ? 0 : _config.GetRuneRemovalCost(weapon.level);

    /// <summary>
    /// Checks whether a specific socket index is unlocked based on the weapon's current level.
    /// </summary>
    public bool IsSocketUnlocked(EquipmentInstance weapon, int socketIndex) =>
        weapon != null && _config.IsSocketUnlocked(weapon.level, socketIndex);

    /// <summary>
    /// Attempts to insert a rune into a weapon socket. 
    /// Validates socket state, checks inventory ownership, consumes the rune from inventory, and updates the socket.
    /// </summary>
    public SocketResult TryInsert(EquipmentInstance weapon, int socketIndex, RuneInstance rune)
    {
        var check = Validate(weapon, socketIndex);
        if (check != SocketResult.Success) return check;

        if (!weapon.sockets[socketIndex].IsEmpty) return SocketResult.SocketOccupied;
        
        if (rune == null || rune.IsEmpty || _data.Inventory.Find<RuneInstance>(rune.instanceId) == null)
            return SocketResult.RuneNotInInventory;

        _data.Inventory.TryRemove(rune.instanceId, out _);
        weapon.sockets[socketIndex] = rune;
        
        OnSocketChanged?.Invoke(weapon, socketIndex);
        return SocketResult.Success;
    }

    /// <summary>
    /// Attempts to remove a rune from a weapon socket.
    /// Verifies inventory space and gem currency BEFORE spending so players never lose currency without receiving the rune back.
    /// </summary>
    public SocketResult TryRemove(EquipmentInstance weapon, int socketIndex)
    {
        var check = Validate(weapon, socketIndex);
        if (check != SocketResult.Success) return check;
        
        var rune = weapon.sockets[socketIndex];
        if (rune.IsEmpty) return SocketResult.SocketEmpty;
        
        // Check inventory space FIRST before deducting gems to prevent loss of currency if inventory is full
        if (_data.Inventory.GetFreeSlots(InventoryCategory.Rune) < 1) return SocketResult.InventoryFull;
        if (!_data.Currency.TrySpend(CurrencyType.Gem, GetRemovalCost(weapon))) return SocketResult.NotEnoughGem;

        _data.Inventory.TryAdd(rune);
        weapon.sockets[socketIndex] = new RuneInstance();
        
        OnSocketChanged?.Invoke(weapon, socketIndex);
        return SocketResult.Success;
    }

    /// <summary>
    /// Validates whether the equipment is a valid weapon category and whether the socket index is within bounds and unlocked.
    /// </summary>
    private SocketResult Validate(EquipmentInstance weapon, int socketIndex)
    {
        if(weapon == null || (weapon.category != InventoryCategory.Melee && weapon.category != InventoryCategory.Ranged))
            return SocketResult.NotWeapon;

        if(socketIndex is < 0 or >= EquipmentInstance.SocketCount) return SocketResult.InvalidSocket;
        if(!_config.IsSocketUnlocked(weapon.level, socketIndex)) return SocketResult.SocketLocked;
        
        weapon.EnsureSockets();
        return SocketResult.Success;
    }
}