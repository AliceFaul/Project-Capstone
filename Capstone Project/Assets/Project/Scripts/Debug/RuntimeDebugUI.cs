using TMPro;
using UnityEngine;

public class RuntimeDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerInfoText;
    [SerializeField] private PlayerRuntime runtime;

    private void Start()
    {
        UpdateUI(EquipmentManager.Instance);
        EquipmentManager.Instance.OnEquipmentChanged += UpdateUI;
    }

    private void UpdateUI(EquipmentManager equipment)
    {
        playerInfoText.text = $"Damage : {runtime.Damage} \n " +
                              $"Attack : {runtime.AttackSpeed} \n" +
                              $"Move : {runtime.MoveSpeed} \n" +
                              $"Defense : {runtime.Defense} \n" +
                              $"Crit Chance : {runtime.CritChance} \n" +
                              $"Crit Damage : {runtime.CritDamage} \n" +
                              $"HP : {runtime.Health}";
    }
}