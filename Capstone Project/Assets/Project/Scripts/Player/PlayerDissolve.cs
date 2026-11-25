using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Death / respawn look of the player model (there are no Dead or get-up clips):
//   DissolveOut: swap every model material to the dissolve material, dissolve it away bottom-up, burst
//                particles, then disable the model.
//   DissolveIn:  the reverse (call PrepareDissolveIn first), then restore the original materials.
// The model is the object that holds the Animator / PlayerAnimationHandler. The Player root stays active so
// the scripts, the camera and the respawn keep working. All timing uses unscaled time.
public class PlayerDissolve : MonoBehaviour
{
    [Tooltip("Empty = PlayerController on this object.")]
    [SerializeField] private PlayerController controller;
    [Tooltip("A material that uses the 'Mhieu/Dissolve Lit' shader. Edge colour, noise scale and so on are tuned on it.")]
    [SerializeField] private Material dissolveMaterial;

    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int DissolveBaseYId = Shader.PropertyToID("_DissolveBaseY");
    private static readonly int DissolveHeightId = Shader.PropertyToID("_DissolveHeight");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // Property names the original materials may use for their texture / colour (URP Lit, then built-in style).
    private static readonly int[] SourceTextureIds = { Shader.PropertyToID("_BaseMap"), Shader.PropertyToID("_MainTex") };
    private static readonly int[] SourceColorIds = { Shader.PropertyToID("_BaseColor"), Shader.PropertyToID("_Color") };

    private class Entry
    {
        public Renderer renderer;
        public Material[] originals;
        public Material[] dissolves;
    }

    private readonly List<Entry> _entries = new List<Entry>();
    private bool _swapped;
    private float _height = 2f;

    public bool IsPresenting { get; private set; }

    private GameObject ModelRoot
    {
        get
        {
            if (controller == null) return null;
            var handler = controller.AnimationHandler;
            return handler != null ? handler.gameObject : null;
        }
    }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
    }

    private void OnDestroy()
    {
        DestroyInstances();
    }

    public IEnumerator DissolveOut(float duration, GameObject particlePrefab, float particleAtProgress)
    {
        IsPresenting = true;

        var root = ModelRoot;
        if (root == null || !SwapMaterials())
        {
            // No model or no dissolve material: just wait, then hide the model.
            yield return new WaitForSecondsRealtime(duration);
            SpawnParticle(particlePrefab);
            if (root != null) root.SetActive(false);
            IsPresenting = false;
            yield break;
        }

        SetAmount(0f);

        bool particlePlayed = false;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetAmount(Mathf.SmoothStep(0f, 1f, progress));

            if (!particlePlayed && progress >= particleAtProgress)
            {
                particlePlayed = true;
                SpawnParticle(particlePrefab);
            }

            yield return null;
        }

        if (!particlePlayed) SpawnParticle(particlePrefab);

        SetAmount(1f);
        root.SetActive(false);
        IsPresenting = false;
    }

    // Makes the model visible again but fully dissolved, so nothing pops in before DissolveIn starts.
    // Call it BEFORE reviving the player (the animation handler only reacts to state changes while enabled).
    public void PrepareDissolveIn()
    {
        IsPresenting = true;

        var root = ModelRoot;
        if (root == null) return;

        if (_swapped) SetAmount(1f);
        root.SetActive(true);
    }

    public IEnumerator DissolveIn(float duration, GameObject particlePrefab)
    {
        IsPresenting = true;

        if (!_swapped)
        {
            RestoreImmediately();
            yield break;
        }

        SpawnParticle(particlePrefab);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetAmount(1f - Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        RestoreMaterials();
        IsPresenting = false;
    }

    // Back to the normal model at once (debug revive, or if something went wrong mid-sequence).
    public void RestoreImmediately()
    {
        var root = ModelRoot;
        if (root != null) root.SetActive(true);

        RestoreMaterials();
        IsPresenting = false;
    }

    private bool SwapMaterials()
    {
        if (_swapped) return true;

        var root = ModelRoot;
        if (root == null || dissolveMaterial == null)
        {
            if (dissolveMaterial == null) Debug.LogWarning("[PlayerDissolve] No dissolve material assigned.", this);
            return false;
        }

        _entries.Clear();
        Bounds bounds = default;
        bool hasBounds = false;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            // Only real meshes: particle and trail renderers must keep their own materials.
            if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;

            Material[] originals = renderer.sharedMaterials;
            var dissolves = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++) dissolves[i] = CreateDissolveInstance(originals[i]);

            renderer.sharedMaterials = dissolves;
            _entries.Add(new Entry { renderer = renderer, originals = originals, dissolves = dissolves });

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (_entries.Count == 0) return false;

        // The dissolve runs from the feet to the head, in world space so the body and the weapon agree.
        _height = Mathf.Max(0.1f, bounds.size.y);
        foreach (var entry in _entries)
        {
            foreach (var material in entry.dissolves)
            {
                material.SetFloat(DissolveBaseYId, bounds.min.y);
                material.SetFloat(DissolveHeightId, _height);
            }
        }

        _swapped = true;
        return true;
    }

    // Copies what the original material looked like (texture, tiling, colour) onto a copy of the dissolve material.
    private Material CreateDissolveInstance(Material original)
    {
        var instance = new Material(dissolveMaterial);
        if (original == null) return instance;

        foreach (int id in SourceTextureIds)
        {
            if (!original.HasProperty(id)) continue;

            instance.SetTexture(BaseMapId, original.GetTexture(id));
            instance.SetTextureScale(BaseMapId, original.GetTextureScale(id));
            instance.SetTextureOffset(BaseMapId, original.GetTextureOffset(id));
            break;
        }

        foreach (int id in SourceColorIds)
        {
            if (!original.HasProperty(id)) continue;

            instance.SetColor(BaseColorId, original.GetColor(id));
            break;
        }

        return instance;
    }

    private void SetAmount(float amount)
    {
        foreach (var entry in _entries)
        {
            foreach (var material in entry.dissolves)
            {
                if (material != null) material.SetFloat(DissolveAmountId, amount);
            }
        }
    }

    private void RestoreMaterials()
    {
        foreach (var entry in _entries)
        {
            if (entry.renderer != null) entry.renderer.sharedMaterials = entry.originals;
        }

        DestroyInstances();
    }

    private void DestroyInstances()
    {
        foreach (var entry in _entries)
        {
            foreach (var material in entry.dissolves)
            {
                if (material != null) Destroy(material);
            }
        }

        _entries.Clear();
        _swapped = false;
    }

    private void SpawnParticle(GameObject prefab)
    {
        if (prefab == null || controller == null) return;

        Vector3 position = controller.transform.position + Vector3.up * (_height * 0.5f);
        GameObject burst = Instantiate(prefab, position, Quaternion.identity);
        Destroy(burst, 5f); // a prefab that does not destroy itself would pile up
    }
}
