using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "New Heal Ability", menuName = "Ability/Heal Ability")]
public class HealAbility : Ability
{
    [SerializeField] private float healAmount = 50f;
    
    public override void Activate(GameObject parent)
    {
        var playerRuntime = parent.GetComponent<PlayerRuntime>();

        if (playerRuntime == null || playerRuntime.IsMaxHealth) return;
        playerRuntime.Heal(healAmount);

        if (VFXPool == null) return;
        var fx = VFXPool.Get();
        fx.transform.position = parent.transform.position;
        parent.GetComponent<MonoBehaviour>().StartCoroutine(ReleaseFX(fx, 2f));
    }

    public override IEnumerator ActivateCoroutine(GameObject parent)
    {
        Activate(parent);
        yield return null;
    }

    private IEnumerator ReleaseFX(GameObject fx, float delay)
    {
        yield return new WaitForSeconds(delay);
        if(fx != null) VFXPool.Release(fx);
    }
}