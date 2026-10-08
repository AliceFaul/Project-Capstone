using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Heal Ability", menuName = "Ability/Heal Ability")]
public class HealAbility : Ability
{
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private float vfxDuration = 2f;
    
    public override void Activate(GameObject parent)
    {
        var playerRuntime = parent.GetComponent<PlayerRuntime>();

        if (playerRuntime == null || playerRuntime.IsMaxHealth) return;
        playerRuntime.Heal(healAmount);
        
        GameObject instanceFx = null;
        if (VFXPool != null)
        {
            instanceFx = VFXPool.Get();
            instanceFx.transform.position = parent.transform.position;
            instanceFx.transform.rotation = parent.transform.rotation;
            instanceFx.transform.SetParent(parent.transform);
        }
        else
        {
            Debug.LogWarning($"[Roll Ability] Not implement VFX prefab to this ability!");
        }
        
        parent.GetComponent<MonoBehaviour>().StartCoroutine(ReleaseFX(instanceFx, vfxDuration));
    }

    public override IEnumerator ActivateCoroutine(GameObject parent)
    {
        Activate(parent);
        yield return null;
    }

    private IEnumerator ReleaseFX(GameObject fx, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (fx == null || VFXPool == null) yield break;
        fx.transform.SetParent(null);
        VFXPool.Release(fx);
    }
}