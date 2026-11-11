using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "AbilityCooldownChannel", menuName = "Ability/Cooldown Channel")]
public class AbilityCooldownChannel : ScriptableObject
{
    public UnityAction<int, float, float> OnCooldownStarted; // (slotIndex, initialTime, maxTime)
    public UnityAction<int> OnAbilityReady; // (slotIndex)

    public void CooldownStarted(int slotIndex, float remaining, float max) => OnCooldownStarted?.Invoke(slotIndex, remaining, max);
    public void AbilityReady(int slotIndex) => OnAbilityReady?.Invoke(slotIndex);
}