using System.Collections.Generic;
using UnityEngine;

public static class ShopText
{
    private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
    {
        { "SHOP_TITLE", "SHOP" }, { "SHOP_BUY", "BUY" }, { "SHOP_SELL", "SELL" },
        { "SHOP_FOR_SALE", "FOR SALE" }, { "SHOP_YOUR_ITEMS", "YOUR ITEMS" },
        { "SHOP_STOCK_NOTE", "Stock refreshes on your next visit to this area." },
        { "SHOP_SELL_NOTE", "Only items in your bag can be sold." },
        { "SHOP_LEFT", "{0} / {1}" }, { "SHOP_MERCHANT", "MERCHANT" },
        { "SHOP_TIP_BUY", "Take a look. You might find something useful." },
        { "SHOP_TIP_SELL", "Have something to sell? Select an item from your bag." },
        { "SHOP_BACK", "Back" }, { "SHOP_SWITCH_TAB", "Switch tab" },
        { "SHOP_COST", "Cost" }, { "SHOP_PURCHASE", "PURCHASE" }, { "SHOP_SOLD", "SOLD" },
        { "SHOP_SELL_PRICE", "Sell price" }, { "SHOP_SELL_BUTTON", "SELL ITEM" },
        { "SHOP_CONFIRM_BUTTON", "CONFIRM SALE" }, { "SHOP_CONFIRM", "Click twice within 3 seconds to sell." },
        { "SHOP_BOUGHT", "Purchased {0}." }, { "SHOP_SOLD_TOAST", "Sold {0} for {1} Gold." },
        { "SHOP_NOT_ENOUGH_GOLD", "Not enough Gold." }, { "SHOP_BAG_FULL", "The bag is full." },
        { "SHOP_ALREADY_SOLD", "This item has already been sold." },
        { "SHOP_CANNOT_SELL", "This item cannot be sold." }
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void RegisterDefaults()
    {
        foreach (var pair in Defaults)
            if (!InventoryText.Defaults.ContainsKey(pair.Key)) InventoryText.Defaults.Add(pair.Key, pair.Value);
    }
}