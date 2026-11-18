using System;
using System.Collections.Generic;
using Project.Capstone.Inventory;

public enum ShopResult { Success, NotEnoughGold, BagFull, AlreadySold, NotSellable }

public class ShopOffer
{
    public IInventoryItem Item;
    public ItemData Definition;
    public int Price;
    public bool Sold;
}

public static class ItemQuality
{
    public static float Get(IInventoryItem item, ItemData definition)
    {
        switch (item)
        {
            case RuneInstance rune:
                var data = definition as RuneData;
                return data != null ? RuneResolver.GetQuality(rune, data) : 0.5f;
            case EquipmentInstance equipment:
                if (equipment.rolls == null || equipment.rolls.Count == 0) return 0.5f;
                float sum = 0f;
                foreach (var roll in equipment.rolls) sum += roll.tier / (float)(StatTierTable.TierCount - 1);
                return sum / equipment.rolls.Count;
            default:
                return 0.5f;
        }
    }
}

// One merchant visit: the generated stock plus buy / sell rules. A new ShopService is generated every time the shop node is entered.
public class ShopService
{
    private readonly PlayerDataConfig _data;
    private readonly ShopPriceConfig _prices;
    private readonly List<ShopOffer> _offers = new List<ShopOffer>();

    public IReadOnlyList<ShopOffer> Offers => _offers;
    public event Action Changed; // after a successful buy or sell

    public ShopService(PlayerDataConfig data, ShopPriceConfig prices)
    {
        _data = data;
        _prices = prices;
    }

    public static ShopService Generate(PlayerDataConfig data, ShopCatalog catalog, ShopPriceConfig prices, int difficulty, System.Random rng = null)
    {
        rng ??= new System.Random();
        var shop = new ShopService(data, prices);
        var config = EquipmentManager.Instance != null ? EquipmentManager.Instance.ProgressConfig : null;

        foreach (var definition in catalog.PickItems(difficulty, rng))
        {
            var item = LootService.CreateItem(definition, config, rng);
            if (item == null) continue;

            shop._offers.Add(new ShopOffer
            {
                Item = item,
                Definition = definition,
                Price = prices.RollBuyPrice(definition.rarity, ItemQuality.Get(item, definition), rng)
            });
        }
        return shop;
    }

    public ShopResult TryBuy(ShopOffer offer)
    {
        if (offer == null || offer.Sold) return ShopResult.AlreadySold;

        var inventory = _data.Inventory;
        if (inventory.GetFreeSlots(offer.Item.Category) <= 0) return ShopResult.BagFull; // checked first: never pay without getting the item
        if (!_data.Currency.TrySpend(CurrencyType.Gold, offer.Price)) return ShopResult.NotEnoughGold;

        if (!inventory.TryAdd(offer.Item))
        {
            _data.Currency.Set(CurrencyType.Gold, _data.Currency.Gold() + offer.Price); // refund (not counted as gold obtained)
            return ShopResult.BagFull;
        }

        offer.Sold = true;
        Changed?.Invoke();
        return ShopResult.Success;
    }

    // Only items lying in a bag can be sold - equipped items and socketed runes are not in the bags.
    public bool CanSell(IInventoryItem item) => item != null && _data.Inventory.Find(item.InstanceId) != null;

    public int GetSellPrice(IInventoryItem item)
    {
        var definition = DefinitionOf(item);
        if (definition == null) return 0;

        int level = item is EquipmentInstance equipment ? equipment.level : 1;
        return _prices.GetSellPrice(definition.rarity, ItemQuality.Get(item, definition), level);
    }

    public ShopResult TrySell(IInventoryItem item, out int price)
    {
        price = 0;
        if (!CanSell(item)) return ShopResult.NotSellable;

        price = GetSellPrice(item);
        if (price <= 0 || !_data.Inventory.TryRemove(item.InstanceId, out _)) return ShopResult.NotSellable;

        _data.Currency.Add(CurrencyType.Gold, price);
        Changed?.Invoke();
        return ShopResult.Success;
    }

    public ItemData DefinitionOf(IInventoryItem item)
    {
        string id = null;
        switch (item)
        {
            case EquipmentInstance e: id = e.definitionId; break;
            case ArtifactInstance a: id = a.definitionId; break;
            case RuneInstance r: id = r.runeId; break;
        }
        return string.IsNullOrEmpty(id) ? null : _data.GetDefinition<ItemData>(id);
    }
}