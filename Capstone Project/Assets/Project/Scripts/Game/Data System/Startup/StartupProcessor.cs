using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StartupProcessor : MonoBehaviour
{
    public static StartupProcessor Instance { get; private set; }

    [SerializeField] private float timeout = 10f;
    [SerializeField] private MonoBehaviour loadingScreen;
    
    private StartupList _startupList;
    private IServiceRegistry _serviceRegistry;
    private ILoading _loading;

    private CancellationTokenSource _cts;

    private InputSystem_Actions _input;
    private bool _isCompleted = false;
    private bool _isLoadingScene = false;

    private bool _offlineMode = false;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _input = new InputSystem_Actions();
        _input.UI.Enable();
        _input.UI.Click.performed += ctx => WaitForClicked();
        
        _loading = loadingScreen as ILoading;
        _loading?.Show();
        
        AsyncOperationHandle<StartupList> handle = Addressables.LoadAssetAsync<StartupList>("StartupList");
        _startupList = await handle.Task;
        
        _serviceRegistry = new ServiceRegistry();
        _cts = new CancellationTokenSource();
        
        var pipelineResult = await RunAllSteps(_cts.Token);
        if (pipelineResult.IsSuccess)
        {
            Debug.Log("[StartupProcessor] Startup Complete");
            _isCompleted = true;
        }
        else
        {
            Debug.LogWarning($"[StartupProcessor] Startup Failed, Error id = {pipelineResult.ErrorId}");
            var errorHandler = new StartupErrorController();
            errorHandler.ThrowError(pipelineResult.ErrorId);
        }
    }

    private struct StartupPipeline
    {
        public bool IsSuccess;
        public string ErrorId;
        public string Message;
        
        public static StartupPipeline Success() => new StartupPipeline { IsSuccess = true };
        public static StartupPipeline Failure(string errorId, string message) => new StartupPipeline { IsSuccess = false,  ErrorId = errorId, Message = message };
    }

    private async Task<StartupPipeline> RunAllSteps(CancellationToken ct)
    {
        if (_startupList.steps.Count == 0)
        {
            Debug.LogWarning($"[StartupProcessor] No steps to run!");
            return StartupPipeline.Failure("NO_STEPS", "No steps configured!");
        }

        int index = 0;
        _loading?.SetProgress(0f, "Running startup system...");
        while (index < _startupList.steps.Count)
        {
            var step = _startupList.steps[index];
            string stepName = step.GetType().Name;

            if (_offlineMode && step.RequiresNetwork)
            {
                Debug.Log($"[StartupProcessor] Skipping network step in offline mode {stepName}");
                index++;
                continue;
            }
            
            Debug.Log($"[StartupProcessor] Running step {stepName}");

            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));
                try
                {
                    CancellationToken effectiveCt = step.HasTimeout ? CancellationToken.None : timeoutCts.Token;
                    var result = await step.RunTasks(_serviceRegistry, effectiveCt);

                    if (!result.IsSuccess)
                    {
                        Debug.LogWarning($"[StartupProcessor] Step failed: {stepName}, error id: {result.ErrorId}, message: {result.Message}");
                        _loading?.SetProgress(1f, "Step failed");
                        return StartupPipeline.Failure(result.ErrorId ?? "STEP_FAILED",
                            $"Step {stepName} failed: {result.Message}");
                    }
                    else
                    {
                        Debug.Log($"[StartupProcessor] Step succeeded: {stepName}");
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning($"[StartupProcessor] Step timeout or cancelled: {stepName}");
                    _loading?.SetProgress(1f, "Step timeout error");
                    return StartupPipeline.Failure("STEP_TIMEOUT", $"Step {stepName} timed out");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return StartupPipeline.Failure("EXCEPTION_" + e.GetType().Name, e.Message);
                }
            }

            index++;
            _loading?.SetProgress((float)index / _startupList.steps.Count);
        }
        
        return StartupPipeline.Success();
    }

    private bool _isRetryClicked = false;
    private bool _isContinueClicked = false;

    private void OnNetworkRetryClicked() => _isRetryClicked = true;
    private void OnContinueClicked() => _isContinueClicked = true;

    private async Task WaitForNetwork()
    {
        while (!_isRetryClicked && !_isContinueClicked)
        {
            await Task.Yield();
        }
    }

    private async void WaitForClicked()
    {
        if (!_isCompleted || _isLoadingScene) return;
        _isLoadingScene = true;
        // Disable startup progress ui
        _input.UI.Disable();
    }

    public TService GetService<TService>()
    {
        return _serviceRegistry.Get<TService>();
    }

    public bool GetService<TService>(out TService service)
    {
        return _serviceRegistry.TryGet<TService>(out service);
    }
}