using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class RuntimeDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerInfoText;
    [SerializeField] private PlayerRuntime runtime;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerController controller;
    
    private IStateMachine _stateMachine;
    private PlayerModifier _modifier;
    
    private void Start()
    {
        if(controller != null)
        {
            if (_stateMachine == null && _modifier == null)
            {
                _stateMachine = controller.StateMachine;
                _modifier = controller.PlayerModifier;
            }
            else
            {
                Debug.Log("[Runtime Debug UI]: State Machine is already assigned");
                Debug.Log("[Runtime Debug UI]: Modifier is already assigned");
            }
        }
        else
        {
            Debug.LogError("[Runtime Debug UI]: Player controller is not assigned");
        }
    }

    private void Update()
    {
        StartCoroutine(UpdateRoutine());
    }

    private IEnumerator UpdateRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateUI();
    }

    private void UpdateUI()
    {
        string target = combat.CurrentTarget != null ? combat.CurrentTarget.name : "None";
        
        string infoText =
            $"===== PLAYER ===== \n\n" +
            $"State: {_stateMachine.CurrentState} \n" +
            $"Health: {runtime.Health} \n\n" +
            $"===== COMBAT ===== \n" +
            $"Damage: {runtime.Damage} \n" +
            $"Attack Speed: {runtime.AttackSpeed} \n" +
            $"Range: {runtime.AttackRange} \n\n" +
            $"===== MOVEMENT ===== \n" +
            $"Move Speed: {runtime.MoveSpeed} \n\n" +
            $"===== DEFENSE ===== \n" +
            $"Defense: {runtime.Defense} \n\n" +
            $"===== CRITICAL ===== \n" +
            $"Crit Chance: {runtime.CritChance} \n" +
            $"Crit Damage: {runtime.CritDamage} \n\n" +
            $"===== STATUS ===== \n" +
            $"Can Move: {_modifier.CanMove} \n" +
            $"Can Attack: {_modifier.CanAttack} \n\n" +
            $"===== TARGET ===== \n" +
            $"Current Target: " + target;
        
        playerInfoText.text = infoText;
    }
}