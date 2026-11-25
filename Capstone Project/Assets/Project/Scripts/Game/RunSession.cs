using System;
using System.Collections;
using UnityEngine;

public enum RunState { Playing, Dying, Respawning, Ended }
public enum RunResult { None, Victory, Defeat }

public class RunSession : MonoBehaviour
{
    public static RunSession Instance { get; private set; }

    [SerializeField] private RunConfig config;
    [Tooltip("Empty = found in the scene.")]
    [SerializeField] private DeathController deathController;
    [Tooltip("Empty = on the same object as the DeathController.")]
    [SerializeField] private PlayerDissolve dissolve;
    [Tooltip("The 'lives left' screen. Empty = found in the scene; if there is none one with the default look is created.")]
    [SerializeField] private DeathOverlay deathOverlay;
    [Tooltip("Shows the lives and the state in the corner of the Game view (for testing).")]
    [SerializeField] private bool showDebugLabel = true;

    public int Lives { get; private set; }
    public int MaxLives => config != null ? config.maxLives : 3;
    public RunState State { get; private set; } = RunState.Playing;
    public RunResult Result { get; private set; } = RunResult.None;
    public int Deaths { get; private set; }
    public float PlayTime { get; private set; } // game time of the run, pauses excluded

    public event Action<int, int> OnLivesChanged;   // lives left, max lives
    public event Action OnPlayerRespawned;
    public event Action<RunResult> OnRunEnded;

    private LevelRunController _run;
    private Vector3 _startPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Lives = MaxLives;
    }

    private IEnumerator Start()
    {
        if (config == null)
        {
            Debug.LogError("[RunSession] No RunConfig assigned.", this);
            enabled = false;
            yield break;
        }

        while (deathController == null)
        {
            deathController = FindFirstObjectByType<DeathController>();
            if (deathController == null) yield return null;
        }

        if (dissolve == null) dissolve = deathController.GetComponent<PlayerDissolve>();
        if (dissolve == null) Debug.LogWarning("[RunSession] No PlayerDissolve on the player: the model will not dissolve.", this);

        if (deathOverlay == null) deathOverlay = FindFirstObjectByType<DeathOverlay>();
        if (deathOverlay == null) deathOverlay = gameObject.AddComponent<DeathOverlay>();

        _startPosition = deathController.transform.position;
        deathController.OnPlayerDied += HandlePlayerDied;

        _run = LevelRunController.Instance;
        if (_run != null) _run.RunCompleted += HandleRunCompleted;

        Lives = MaxLives;
        OnLivesChanged?.Invoke(Lives, MaxLives);
    }

    private void Update()
    {
        if (State != RunState.Ended) PlayTime += Time.deltaTime;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (deathController != null) deathController.OnPlayerDied -= HandlePlayerDied;
        if (_run != null) _run.RunCompleted -= HandleRunCompleted;
    }

    private void HandlePlayerDied()
    {
        // Ignore a second death signal while a sequence is running or after the run is over.
        if (State != RunState.Playing) return;
        StartCoroutine(DeathSequence());
    }

    private void HandleRunCompleted()
    {
        if (State == RunState.Ended) return;
        EndRun(RunResult.Victory);
    }

    private IEnumerator DeathSequence()
    {
        State = RunState.Dying;
        Deaths++;
        Lives = Mathf.Max(0, Lives - 1);
        bool lastLife = Lives <= 0;
        OnLivesChanged?.Invoke(Lives, MaxLives);

        var audio = AudioService.Instance;
        if (audio != null)
        {
            audio.PlaySfx(config.deathCue, deathController.transform.position);
            // Higher priority than the scene music: it fades out into the death music, and popping this entry
            // later brings the scene music back where it was.
            audio.PushMusic(config.deathTrack, MusicPriority.Cinematic, this, config.musicFade);
        }

        if (dissolve != null)
            yield return StartCoroutine(dissolve.DissolveOut(config.dissolveDuration, config.deathParticle, config.particleAtProgress));
        else
            yield return new WaitForSecondsRealtime(config.dissolveDuration);

        yield return StartCoroutine(FadeRoutine(1f, config.fadeToBlack));
        AudioService.Instance?.PlaySfx(config.lifeLostCue);

        if (lastLife)
        {
            // The game over screen stays up until the run summary (next phase) takes over.
            if (deathOverlay != null) yield return StartCoroutine(deathOverlay.PresentGameOver(MaxLives));
            EndRun(RunResult.Defeat);
            yield break;
        }

        // The overlay takes config.blackHold seconds (fade in, the lost life pops, hold, fade out).
        if (deathOverlay != null) yield return StartCoroutine(deathOverlay.Present(Lives, MaxLives, config.blackHold));
        else yield return new WaitForSecondsRealtime(config.blackHold);

        yield return StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        State = RunState.Respawning;

        // Never respawn in the middle of a node change: the area (and its entry point) is about to change.
        while (_run != null && _run.IsTransitioning) yield return null;

        Vector3 position = GetRespawnPosition();
        Vector3 before = deathController.transform.position;

        // The model must be active (but fully dissolved) before the revive: the animation handler only
        // listens to state changes while it is enabled.
        if (dissolve != null) dissolve.PrepareDissolveIn();

        if (!deathController.Revive(position, config.reviveHealthFraction))
            Debug.LogWarning("[RunSession] Revive was refused: the player was not dead.", this);

        CameraController.Instance?.SnapToTarget(deathController.transform.position - before);

        AudioService.Instance?.PopMusic(this, config.musicFade);
        AudioService.Instance?.PlaySfx(config.respawnCue, deathController.transform.position);

        Coroutine fade = StartCoroutine(FadeRoutine(0f, config.fadeFromBlack));

        if (dissolve != null)
        {
            GameObject particle = config.respawnParticle != null ? config.respawnParticle : config.deathParticle;
            yield return StartCoroutine(dissolve.DissolveIn(config.materializeDuration, particle));
        }

        yield return fade;

        State = RunState.Playing;
        OnPlayerRespawned?.Invoke();
    }

    // The entry point of the area the player is in (the start of the current map node). Enemies that were
    // already killed stay dead because the area is not reset.
    private Vector3 GetRespawnPosition()
    {
        var area = _run != null ? _run.ActiveArea : null;
        return area != null ? area.EntryPosition : _startPosition;
    }

    private void EndRun(RunResult result)
    {
        State = RunState.Ended;
        Result = result;

        // Replaces the death track (same owner) when the run is lost.
        var track = result == RunResult.Victory ? config.victoryTrack : config.defeatTrack;
        AudioService.Instance?.PushMusic(track, MusicPriority.Cinematic, this, config.musicFade);

        if (result == RunResult.Defeat)
        {
            _run?.Fail();
            SaveHelper.SaveNow(); // keep everything earned during the run (gold, exp, items are applied at once)
        }

        Debug.Log($"[RunSession] Run ended: {result}. Deaths: {Deaths}, time: {PlayTime:F0}s.");
        OnRunEnded?.Invoke(result);
    }

    // The fader may be missing in a test scene: then just wait the same time.
    private IEnumerator FadeRoutine(float alpha, float duration)
    {
        var fader = ScreenFader.Instance;
        if (fader == null)
        {
            yield return new WaitForSecondsRealtime(duration);
            yield break;
        }

        yield return fader.FadeTo(alpha, duration);
    }

    private void OnGUI()
    {
        if (!showDebugLabel) return;
        GUI.Label(new Rect(10f, 10f, 360f, 24f), $"Lives {Lives}/{MaxLives}   {State}   {Result}");
    }
}
