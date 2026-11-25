using System.Collections;
using TMPro;
using UnityEngine;

public class AreaExit : Interactable
{
    [Header("Presentation")]
    [Tooltip("Optional world-space label showing the type of the next node.")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject openVisual;

    [Tooltip("Order of this exit: exit 0 leads to the first next node, exit 1 to the second...")]
    [SerializeField] private int exitIndex;

    [Header("Reveal on clear")]
    [Tooltip("Optional one-shot Particle System prefab. Leave empty for a ring of mint sparks.")]
    [SerializeField] private ParticleSystem revealVfxPrefab;
    [Tooltip("Open the exit after this many gameplay seconds even if the effect is still playing.")]
    [SerializeField, Min(0.1f)] private float maxRevealTime = 3f;

    public int ExitIndex { get => exitIndex; set => exitIndex = value; }
    public int TargetNodeId { get; private set; } = -1;
    public NodeType TargetType { get; private set; }
    public bool Locked { get; private set; } = true;

    private Transform _camera;
    private ParticleSystem _activeVfx;
    private bool _revealing;
    private int _revision;

    public void Bind(NodeType targetType, int targetNodeId, bool locked)
    {
        TargetType = targetType;
        TargetNodeId = targetNodeId;
        if (label != null) label.text = InventoryText.Get("NODE_" + targetType.ToString().ToUpper());
        SetLocked(locked);
    }

    public void Unbind()
    {
        TargetNodeId = -1;
        SetLocked(true);
    }

    public void SetLocked(bool locked)
    {
        _revision++;
        _revealing = false;
        if (_activeVfx != null)
        {
            _activeVfx.gameObject.SetActive(false);
            Destroy(_activeVfx.gameObject);
            _activeVfx = null;
        }

        Locked = locked || TargetNodeId < 0;
        if (lockedVisual != null && lockedVisual != gameObject) lockedVisual.SetActive(false);
        if (openVisual != null && openVisual != gameObject) openVisual.SetActive(!Locked);
        gameObject.SetActive(!Locked);
    }

    // Call with AreaController.StartCoroutine. The VFX is a child of the area, outside the hidden exit.
    public IEnumerator RevealAfterClear(Transform area)
    {
        if (TargetNodeId < 0 || !Locked || _revealing) yield break;
        _revealing = true;
        int revision = _revision;

        // Immediate nodes can clear while the screen is still fading in.
        var run = LevelRunController.Instance;
        while (revision == _revision && run != null && run.IsTransitioning) yield return null;
        if (revision != _revision) yield break;

        try
        {
            _activeVfx = PlayRevealVfx(area);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }

        float remaining = Mathf.Max(0.1f, maxRevealTime);
        while (revision == _revision && _activeVfx != null && _activeVfx.gameObject.activeInHierarchy
               && _activeVfx.IsAlive(true) && remaining > 0f)
        {
            yield return null;
            remaining -= Time.deltaTime;
        }

        if (revision == _revision) SetLocked(false);
    }

    private ParticleSystem PlayRevealVfx(Transform area)
    {
        if (revealVfxPrefab == null) return PlayDefaultBurst(area);

        var effect = Instantiate(revealVfxPrefab, transform.position, transform.rotation, area);
        foreach (var system in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = system.main;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        }
        effect.gameObject.SetActive(true);
        effect.Play(true);
        return effect;
    }

    private ParticleSystem PlayDefaultBurst(Transform area)
    {
        var go = new GameObject("Exit Reveal");
        go.transform.SetParent(area, false);
        go.transform.SetPositionAndRotation(transform.position + Vector3.up * 0.15f, Quaternion.identity);

        var effect = go.AddComponent<ParticleSystem>();
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = effect.main;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = 0.5f;
        main.startSize = 0.16f;
        main.startColor = new Color(0.48f, 1f, 0.82f);
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = effect.emission;
        emission.enabled = false;
        var shape = effect.shape;
        shape.enabled = false;
        var size = effect.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = Canvas.GetDefaultCanvasMaterial();

        const int sparks = 24;
        effect.Play(false);
        for (int i = 0; i < sparks; i++)
        {
            float angle = i * 2f * Mathf.PI / sparks;
            var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var spark = new ParticleSystem.EmitParams
            {
                position = outward * 0.35f,
                velocity = outward * 2f + Vector3.up * (i % 2 == 0 ? 1.4f : 2.4f)
            };
            effect.Emit(spark, 1);
        }
        return effect;
    }

    public override bool CanInteract(PlayerController player)
    {
        var run = LevelRunController.Instance;
        return isActiveAndEnabled && !Locked && TargetNodeId >= 0 && run != null
               && !run.IsTransitioning && base.CanInteract(player);
    }

    protected override void OnInteract(PlayerController player) => LevelRunController.Instance.Choose(TargetNodeId);

    private void LateUpdate()
    {
        if (label == null) return;
        if (_camera == null && Camera.main != null) _camera = Camera.main.transform;
        if (_camera != null) label.transform.rotation = _camera.rotation;
    }
}