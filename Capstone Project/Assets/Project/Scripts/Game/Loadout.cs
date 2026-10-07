using System.Collections.Generic;
using System;

namespace Project.Capstone.Inventory
{
    public enum EquipSlot { Melee, Ranged, Armor, Artifact }

    [Serializable]
    public class LoadoutData
    {
        public string meleeId;
        public string rangedId;
        public string armorId;
        public List<string> artifactIds = new List<string>();
    }

    public class Loadout
    {
        public const int ArtifactSlotCount = 3;
 
        private readonly Inventory _inventory;
        private EquipmentInstance _melee;
        private EquipmentInstance _ranged;
        private EquipmentInstance _armor;
        private readonly ArtifactInstance[] _artifacts = new ArtifactInstance[ArtifactSlotCount];
 
        // (slot, index artifact, do cu, do moi)
        public event Action<EquipSlot, int, IInventoryItem, IInventoryItem> OnChanged;
        // Sau khi nap tu file save/cloud - nhieu slot doi cung luc nen chi bao 1 lan.
        public event Action OnReloaded;
 
        public Loadout(Inventory inventory)
        {
            _inventory = inventory;
        }
 
        public EquipmentInstance Melee => _melee;
        public EquipmentInstance Ranged => _ranged;
        public EquipmentInstance Armor => _armor;
        public ArtifactInstance GetArtifact(int index) => index >= 0 && index < ArtifactSlotCount ? _artifacts[index] : null;
 
        public IInventoryItem Get(EquipSlot slot, int index = 0)
        {
            switch (slot)
            {
                case EquipSlot.Melee: return _melee;
                case EquipSlot.Ranged: return _ranged;
                case EquipSlot.Armor: return _armor;
                case EquipSlot.Artifact: return GetArtifact(index);
                default: return null;
            }
        }
 
        public static bool TrySlotFor(IInventoryItem item, out EquipSlot slot)
        {
            slot = default;
            if (item == null) return false;
 
            switch (item.Category)
            {
                case InventoryCategory.Melee: slot = EquipSlot.Melee; return true;
                case InventoryCategory.Ranged: slot = EquipSlot.Ranged; return true;
                case InventoryCategory.Armor: slot = EquipSlot.Armor; return true;
                case InventoryCategory.Artifact: slot = EquipSlot.Artifact; return true;
                default: return false; // Rune khong "mac" - rune duoc gan vao socket (Phase B)
            }
        }
 
        // artifactIndex < 0: tu chon o artifact trong dau tien (het cho thi thay o 0).
        public bool TryEquip(IInventoryItem item, int artifactIndex = -1)
        {
            if (!TrySlotFor(item, out var slot)) return false;
            if (_inventory.Find(item.InstanceId) == null) return false; // phai dang nam trong tui
 
            int index = 0;
            if (slot == EquipSlot.Artifact)
            {
                index = artifactIndex >= 0 && artifactIndex < ArtifactSlotCount ? artifactIndex : FirstEmptyArtifactSlot();
                if (index < 0) index = 0;
            }
 
            var old = Get(slot, index);
 
            _inventory.TryRemove(item.InstanceId, out _);
            Set(slot, index, item);
 
            if (old != null && !_inventory.TryAdd(old))
            {
                // Khong the xay ra (vua giai phong 1 o cung loai) - hoan tac cho an toan.
                Set(slot, index, old);
                _inventory.TryAdd(item);
                return false;
            }
 
            OnChanged?.Invoke(slot, index, old, item);
            return true;
        }
 
        // Thao do ve tui. That bai neu tui loai do da day.
        public bool TryUnequip(EquipSlot slot, int index = 0)
        {
            var item = Get(slot, index);
            if (item == null) return false;
            if (!_inventory.TryAdd(item)) return false;
 
            Set(slot, index, null);
            OnChanged?.Invoke(slot, index, item, null);
            return true;
        }
 
        public IEnumerable<IInventoryItem> EquippedItems()
        {
            if (_melee != null) yield return _melee;
            if (_ranged != null) yield return _ranged;
            if (_armor != null) yield return _armor;
            foreach (var artifact in _artifacts)
            {
                if (artifact != null) yield return artifact;
            }
        }
 
        public LoadoutData ToData()
        {
            var data = new LoadoutData
            {
                meleeId = _melee?.instanceId,
                rangedId = _ranged?.instanceId,
                armorId = _armor?.instanceId
            };
            foreach (var artifact in _artifacts) data.artifactIds.Add(artifact?.instanceId ?? string.Empty);
            return data;
        }
 
        public void Apply(LoadoutData data, Func<string, IInventoryItem> resolve)
        {
            _melee = Resolve<EquipmentInstance>(data?.meleeId, InventoryCategory.Melee, resolve);
            _ranged = Resolve<EquipmentInstance>(data?.rangedId, InventoryCategory.Ranged, resolve);
            _armor = Resolve<EquipmentInstance>(data?.armorId, InventoryCategory.Armor, resolve);
 
            for (int i = 0; i < ArtifactSlotCount; i++)
            {
                string id = data != null && data.artifactIds != null && i < data.artifactIds.Count ? data.artifactIds[i] : null;
                _artifacts[i] = ResolveArtifact(id, resolve);
            }
 
            OnReloaded?.Invoke();
        }
 
        private static T Resolve<T>(string id, InventoryCategory category, Func<string, IInventoryItem> resolve) where T : class, IInventoryItem
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            var item = resolve(id);
            if(item is not EquipmentInstance equipment) return null;
            if(equipment.Category != category) return null;
            return equipment as T;
        }

        private static ArtifactInstance ResolveArtifact(string id, Func<string, IInventoryItem> resolve)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var item = resolve(id);
            return item is ArtifactInstance { Category: InventoryCategory.Artifact } artifact ? artifact : null;
        }
 
        private int FirstEmptyArtifactSlot()
        {
            for (int i = 0; i < ArtifactSlotCount; i++)
            {
                if (_artifacts[i] == null) return i;
            }
            return -1;
        }
 
        private void Set(EquipSlot slot, int index, IInventoryItem item)
        {
            switch (slot)
            {
                case EquipSlot.Melee: _melee = item as EquipmentInstance; break;
                case EquipSlot.Ranged: _ranged = item as EquipmentInstance; break;
                case EquipSlot.Armor: _armor = item as EquipmentInstance; break;
                case EquipSlot.Artifact: _artifacts[index] = item as ArtifactInstance; break;
            }
        }
    }
}