using TMPro;
using UnityEngine;

// "Player Stats" panel. Row order: Health, Damage, Defense, Move Speed, Crit Chance, Crit Damage, Attack Speed.
public class PlayerStatsView : MonoBehaviour
{
    [SerializeField] private TMP_Text[] values;

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        if (values == null || values.Length < 7) return;

        var player = FindFirstObjectByType<PlayerRuntime>();
        if (player == null)
        {
            foreach (var v in values) v.text = "-";
            return;
        }

        values[0].text = player.TotalHealth.ToString();
        values[1].text = player.TotalDamage.ToString();
        values[2].text = player.TotalArmor.ToString();
        values[3].text = player.TotalSpeed.ToString("0.##");
        values[4].text = $"{player.TotalCritChance:0.#}%";
        values[5].text = $"{player.TotalCritDamage:0.#}%";
        values[6].text = $"x{player.ModifyStat(BonusStat.AttackSpeed, 1f):0.##}"; // multiplier from equipment/buffs
    }
}
