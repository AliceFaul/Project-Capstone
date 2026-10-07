using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project.Capstone.Inventory
{
    // Moi loai co tui rieng. Them loai moi = them 1 gia tri enum nay + 1 dong trong InventoryConfig
    // (khong sua logic). Chi them 1 LOAI VAT PHAM moi (khac EquipmentInstance/RuneInstance/
    // ArtifactInstance) moi can them code luu/doc trong PlayerDataConfig.
    public enum InventoryCategory { Melee, Ranged, Armor, Rune, Artifact }

    public interface IInventoryItem
    {
        string InstanceId { get; }
        InventoryCategory Category { get; }
    }

    [Serializable]
    public class CategoryCapacity
    {
        public InventoryCategory category;
        public int slotCount = 25;
    }

    // Bo cuc 1 tui de luu: thu tu o -> instanceId ("" = o trong).
    [Serializable]
    public class BagLayout
    {
        public InventoryCategory category;
        public List<string> slotInstanceIds = new List<string>();
    }

    public class Inventory
    {
        private readonly Dictionary<InventoryCategory, IInventoryItem[]> _bags = new Dictionary<InventoryCategory, IInventoryItem[]>();

        public event Action<InventoryCategory> OnBagChanged;

        public Inventory(IEnumerable<CategoryCapacity> capacities)
        {
            foreach (var capacity in capacities)
            {
                _bags[capacity.category] = new IInventoryItem[Mathf.Max(0, capacity.slotCount)];
            }
        }

        public bool HasCategory(InventoryCategory category) => _bags.ContainsKey(category);

        public int GetCapacity(InventoryCategory category) => _bags.TryGetValue(category, out var bag) ? bag.Length : 0;

        // Danh sach o (phan tu null = o trong) - UI chi doc, khong sua truc tiep.
        public IReadOnlyList<IInventoryItem> GetSlots(InventoryCategory category)
        {
            return _bags.TryGetValue(category, out var bag) ? bag : Array.Empty<IInventoryItem>();
        }

        public int GetFreeSlots(InventoryCategory category)
        {
            if (!_bags.TryGetValue(category, out var bag)) return 0;
            int free = 0;
            foreach (var slot in bag)
            {
                if (slot == null) free++;
            }
            return free;
        }

        // Kiem tra TRUOC khi them nhieu mon (vi du gacha x10): dem so o can THEO TUNG LOAI, khong
        // phai tong so o trong - 10 kiem can 10 o trong rieng o tui Melee.
        public bool CanFit(IEnumerable<IInventoryItem> items)
        {
            foreach (var group in items.GroupBy(i => i.Category))
            {
                if (GetFreeSlots(group.Key) < group.Count()) return false;
            }
            return true;
        }

        public bool TryAdd(IInventoryItem item)
        {
            if (item == null) return false;

            if (!_bags.TryGetValue(item.Category, out var bag))
            {
                Debug.LogWarning($"[Inventory] Chua cau hinh tui cho loai {item.Category}.");
                return false;
            }

            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] != null) continue;
                bag[i] = item;
                OnBagChanged?.Invoke(item.Category);
                return true;
            }

            return false;
        }

        // Tat ca hoac khong: neu khong du cho cho TOAN BO thi khong them mon nao.
        public bool TryAddRange(IReadOnlyCollection<IInventoryItem> items)
        {
            if (!CanFit(items)) return false;
            foreach (var item in items) TryAdd(item);
            return true;
        }

        public bool TryRemove(string instanceId, out IInventoryItem removed)
        {
            removed = null;
            if (string.IsNullOrEmpty(instanceId)) return false;

            foreach (var pair in _bags)
            {
                var bag = pair.Value;
                for (int i = 0; i < bag.Length; i++)
                {
                    if (bag[i] == null || bag[i].InstanceId != instanceId) continue;
                    removed = bag[i];
                    bag[i] = null;
                    OnBagChanged?.Invoke(pair.Key);
                    return true;
                }
            }

            return false;
        }

        public IInventoryItem Find(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            return AllItems().FirstOrDefault(i => i.InstanceId == instanceId);
        }

        public T Find<T>(string instanceId) where T : class, IInventoryItem => Find(instanceId) as T;

        public IEnumerable<IInventoryItem> AllItems()
        {
            foreach (var bag in _bags.Values)
            {
                foreach (var slot in bag)
                {
                    if (slot != null) yield return slot;
                }
            }
        }

        public List<BagLayout> ToLayouts()
        {
            var layouts = new List<BagLayout>();
            foreach (var pair in _bags)
            {
                var layout = new BagLayout { category = pair.Key };
                foreach (var slot in pair.Value) layout.slotInstanceIds.Add(slot != null ? slot.InstanceId : string.Empty);
                layouts.Add(layout);
            }
            return layouts;
        }

        // Day du lieu da luu vao CHINH instance Inventory nay (khong tao moi) de cac noi da dang ky
        // OnBagChanged khong bi mat ket noi. 'resolve' tra ve item theo instanceId (null neu khong co).
        public void ApplyLayouts(IEnumerable<BagLayout> layouts, Func<string, IInventoryItem> resolve)
        {
            foreach (var bag in _bags.Values) Array.Clear(bag, 0, bag.Length);

            var overflow = new List<IInventoryItem>();

            if (layouts != null)
            {
                foreach (var layout in layouts)
                {
                    if (!_bags.TryGetValue(layout.category, out var bag)) continue;

                    for (int i = 0; i < layout.slotInstanceIds.Count; i++)
                    {
                        string id = layout.slotInstanceIds[i];
                        if (string.IsNullOrEmpty(id)) continue;

                        var item = resolve(id);
                        if (item == null || item.Category != layout.category) continue;

                        if (i < bag.Length) bag[i] = item;
                        else overflow.Add(item); // config da bi giam so o so voi luc luu
                    }
                }
            }

            foreach (var item in overflow)
            {
                if (!TryAdd(item)) Debug.LogWarning($"[Inventory] Het cho, mat item {item.InstanceId} sau khi giam so o.");
            }

            foreach (var category in _bags.Keys) OnBagChanged?.Invoke(category);
        }
    }
}