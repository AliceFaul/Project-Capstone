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
        
        InitializePool();

        if (VFXPool == null) return;
        var fx = VFXPool.Get();
        fx.transform.position = parent.transform.position;
        fx.transform.SetParent(parent.transform);
        parent.GetComponent<MonoBehaviour>().StartCoroutine(ReleaseFX(fx, vfxDuration));
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