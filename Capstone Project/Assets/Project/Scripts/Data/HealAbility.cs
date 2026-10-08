using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Heal Ability", menuName = "Ability/Heal Ability")]
public class HealAbility : Ability
{
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private float vfxDuration = 2f;
    
    public override void Activate(GameObject parent) { }

    public override IEnumerator ActivateCoroutine(GameObject parent)
    {
        if(parent == null) yield break;
        
        var playerRuntime = parent.GetComponent<PlayerRuntime>();
        if(playerRuntime == null) yield break;

        if (playerRuntime.IsMaxHealth) Debug.Log($"[Heal Ability] Player is already at Max health!");
        else playerRuntime.Heal(healAmount);   
        
        GameObject instanceFx = null;
        if (VFXPool != null)
        {
            instanceFx = VFXPool.Get();
            
            if (instanceFx != null)
            {
                instanceFx.transform.position = parent.transform.position + Vector3.up;
                instanceFx.transform.rotation = parent.transform.rotation;
                instanceFx.transform.SetParent(parent.transform);
            }
        }
        else
        {
            Debug.LogWarning($"[Heal Ability] VFX Prefab is missing on this ability asset!");
        }

        if (instanceFx != null)
        {
            yield return new WaitForSeconds(vfxDuration);
            if (instanceFx != null && VFXPool != null)
            {
                instanceFx.transform.SetParent(null);
                VFXPool.Release(instanceFx);
            }
        }
    }
}