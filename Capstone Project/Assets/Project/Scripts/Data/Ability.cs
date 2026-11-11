using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using System.Collections;

public abstract class Ability : ScriptableObject
{
    [Header("Ability Information")]
    [SerializeField] private string abilityName;
    [SerializeField] private float abilityCooldown;
    [SerializeField] private Sprite abilityIcon;
    
    [Header("Ability Input")]
    [SerializeField] private InputActionProperty input;
    
    [Header("VFX")]
    [SerializeField] private GameObject vfxPrefab;
    
    public string AbilityName => abilityName;
    public float AbilityCooldown => abilityCooldown;
    public Sprite AbilityIcon => abilityIcon;
    public InputAction Input => input.action;
    
    protected IObjectPool<GameObject> VFXPool;

    public virtual void InitializePool()
    {
        if(vfxPrefab == null || VFXPool != null) return;
        
        VFXPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(vfxPrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: Destroy,
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 10);
    }

    public abstract void Activate(GameObject parent);
    public abstract IEnumerator ActivateCoroutine(GameObject parent);
}