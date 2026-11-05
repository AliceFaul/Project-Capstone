//using UnityEngine;

//public class GachaManager : MonoBehaviour
//{
//    [Header("References")]
//    [SerializeField] private GachaChestVisual chestVisual;

//    // Giả lập thông số hệ thống (Sẽ liên kết với CurrencySystem và InventorySystem thực tế của team)
//    [SerializeField] private int playerGems = 1000;
//    [SerializeField] private int availableInventorySlots = 15;

//    public void OnClickOpenX1()
//    {
//        // 1. Kiểm tra Gem (Mở x1 = 50 Gem)
//        if (playerGems < 50)
//        {
//            Debug.Log("[UI Popup] Không đủ Gem!");
//            return;
//        }

//        // 2. Kiểm tra túi đồ (Cần ít nhất 1 ô trống)[cite: 7]
//        if (availableInventorySlots < 1)
//        {
//            Debug.Log("[UI Popup] Túi đồ đã đầy!");
//            return;
//        }

//        // 3. Trừ tiền và thực hiện mở
//        playerGems -= 50;
//        Rarity rewardedRarity = RollRarity();

//        if (chestVisual != null)
//            chestVisual.PlayOpenAnimation(rewardedRarity);

//        Debug.Log($"[Gacha] Mở x1 thành công! Nhận được vật phẩm độ hiếm: {rewardedRarity}");
//    }

//    public void OnClickOpenX10()
//    {
//        // 1. Kiểm tra Gem (Mở x10 = 500 Gem)[cite: 7]
//        if (playerGems < 500)
//        {
//            Debug.Log("[UI Popup] Không đủ Gem!");
//            return;
//        }

//        // 2. Kiểm tra túi đồ (Cần tối thiểu 10 ô trống cho x10)[cite: 7]
//        if (availableInventorySlots < 10)
//        {
//            Debug.Log("[UI Popup] Cần tối thiểu 10 ô trống trong túi đồ!");
//            return;
//        }

//        // 3. Trừ tiền và chạy vòng lặp 10 lần bốc thăm[cite: 7]
//        playerGems -= 500;
//        for (int i = 0; i < 10; i++)
//        {
//            Rarity rewardedRarity = RollRarity();
//            Debug.Log($"[Gacha x10] Vật phẩm thứ {i + 1}: {rewardedRarity}");
//        }

//        if (chestVisual != null)
//            chestVisual.PlayOpenAnimation(Rarity.Rare); // Hiệu ứng chung cho x10

//        Debug.Log("[Gacha] Mở loạt x10 thành công! Hiển thị bảng tổng kết 10 ô phần thưởng.");
//    }

//    // Thuật toán bốc thăm theo đúng tỷ lệ trong GDD
//    private Rarity RollRarity()
//    {
//        float roll = Random.Range(0f, 100f);
//        if (roll <= 50f) return Rarity.Common;         // 50%[cite: 7]
//        else if (roll <= 80f) return Rarity.Uncommon;  // 30% (50 + 30)[cite: 7]
//        else if (roll <= 95f) return Rarity.Rare;      // 15% (80 + 15)[cite: 7]
//        else return Rarity.Legendary;                  // 5% còn lại[cite: 7]
//    }
//}

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RarityWeaponPool
{
    public Rarity rarity;
    public List<WeaponData> weapons; // kéo các vũ khí thuộc độ hiếm này vào
}

public class GachaManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GachaChestVisual chestVisual;
    [SerializeField] private GachaResultUI resultUI;

    [Header("Chi phí (theo GDD)")]
    [SerializeField] private int costX1 = 50;
    [SerializeField] private int costX10 = 500;

    [Header("Kho vũ khí theo độ hiếm")]
    [SerializeField] private List<RarityWeaponPool> weaponPools;

    // Giả lập, sẽ nối với Currency / Inventory thật của team
    [SerializeField] private int playerGems = 1000;
    [SerializeField] private int availableInventorySlots = 15;

    private bool isBusy; // chặn bấm liên tục khi đang chạy animation / đang xem kết quả

    public void OnClickOpenX1()
    {
        if (isBusy) return;

        if (playerGems < costX1) { ShowMessage("Không đủ Gem!"); return; }
        if (availableInventorySlots < 1) { ShowMessage("Túi đồ đã đầy!"); return; }

        WeaponData reward = RollWeapon();
        if (reward == null) return; // chưa cấu hình pool, chưa trừ tiền

        playerGems -= costX1;
        AddToInventory(reward);

        isBusy = true;
        chestVisual.PlayOpenAnimation(reward.rarity, () =>
        {
            resultUI.ShowSingle(reward, () =>
            {
                chestVisual.ResetChest();
                isBusy = false;
            });
        });
    }

    public void OnClickOpenX10()
    {
        if (isBusy) return;

        if (playerGems < costX10) { ShowMessage("Không đủ Gem!"); return; }
        if (availableInventorySlots < 10) { ShowMessage("Cần tối thiểu 10 ô trống trong túi đồ!"); return; }

        // Bốc đủ 10 món trước, nếu lỗi cấu hình thì chưa trừ tiền
        var rewards = new List<WeaponData>();
        for (int i = 0; i < 10; i++)
        {
            WeaponData w = RollWeapon();
            if (w == null) return;
            rewards.Add(w);
        }

        playerGems -= costX10;
        foreach (var w in rewards) AddToInventory(w);

        // GDD: x10 bỏ qua từng món, hiện thẳng bảng tổng kết
        isBusy = true;
        resultUI.ShowSummary(rewards, () => isBusy = false);
    }

    private WeaponData RollWeapon()
    {
        Rarity rarity = RollRarity();
        var pool = weaponPools.Find(p => p.rarity == rarity);
        if (pool == null || pool.weapons.Count == 0)
        {
            Debug.LogError($"[Gacha] Chưa có vũ khí nào cho độ hiếm {rarity}!");
            return null;
        }
        return pool.weapons[Random.Range(0, pool.weapons.Count)];

    }
    private Rarity RollRarity()
    {
        float roll = Random.Range(0f, 100f);
        if (roll <= 50f) return Rarity.Common;         // 50%
        else if (roll <= 80f) return Rarity.Uncommon;  // 30% (50 + 30)
        else if (roll <= 95f) return Rarity.Rare;      // 15% (80 + 15)
        else return Rarity.Legendary;                  // 5% còn lại
    }

    //NỐI HỆ THỐNG THẬT
    private void AddToInventory(WeaponData w)
    {
        availableInventorySlots--; // thay bằng PlayerInventory thật
    }

    private void ShowMessage(string msg)
    {
        Debug.Log($"[UI Popup] {msg}"); // thay bằng FloatingTextService
    }
}