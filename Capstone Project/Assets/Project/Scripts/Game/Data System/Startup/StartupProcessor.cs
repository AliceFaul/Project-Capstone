using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

public class StartupProcessor : MonoBehaviour
{
    public static StartupProcessor Instance { get; private set; }

    [SerializeField] private float timeout = 10f;
    [SerializeField] private MonoBehaviour loadingScreen;
    
    private StartupList _startupList;
    private IServiceRegistry _serviceRegistry;
    private ILoading _loading;
    
    private readonly LocalizedString _clickToStartLocale = new LocalizedString("UI", "CLICK_TO_START");
    private readonly LocalizedString _clickToContinueLocale = new LocalizedString("UI", "CLICK_TO_CONTINUE");
    private readonly LocalizedString _runningStartupLocale = new LocalizedString("UI", "RUNNING_STARTUP");
    private readonly LocalizedString _stepFailedLocale = new LocalizedString("UI", "UI_STEP_FAILED");
    private readonly LocalizedString _stepTimeoutLocale = new LocalizedString("UI", "UI_STEP_TIMEOUT");

    private CancellationTokenSource _cts;

    private InputSystem_Actions _input;
    private bool _offlineMode = false;

    private TaskCompletionSource<bool> _clickTcs;

    private async void Awake()
    {
        try
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
            _input.UI.Click.performed += OnClickPerformed;
        
            if(loadingScreen == null)
                loadingScreen = GameObject.FindWithTag("LoadingScreen").GetComponent<MonoBehaviour>();
            _loading = loadingScreen as ILoading;
            
            Debug.Log($"[StartupProcessor] Waiting clicked...");
            if (_loading != null)
            {
                await _loading.HideProgressBar();
                _loading.SetMessage(_clickToStartLocale);
            }

            await WaitForClicked();

            if(_loading != null)
                await _loading.ShowProgressBar();

            AsyncOperationHandle<StartupList> handle = Addressables.LoadAssetAsync<StartupList>("StartupList");
            _startupList = await handle.Task;
        
            _serviceRegistry = new ServiceRegistry();
            _cts = new CancellationTokenSource();
        
            var pipelineResult = await RunAllSteps(_cts.Token);
            if (pipelineResult.IsSuccess)
            {
                Debug.Log("[StartupProcessor] Startup Completed - click to activate Main Menu!");
                _loading?.SetProgress(1f, _clickToContinueLocale);
                await Task.Delay(300);

                if (_loading != null)
                    await _loading.HideProgressBar();
                
                await WaitForClicked();
                
                if (_loading != null)
                    await _loading.Hide();
                OpenMainMenu();
            }
            else
            {
                Debug.LogWarning($"[StartupProcessor] Startup Failed, Error id = {pipelineResult.ErrorId}");
                var errorHandler = new StartupErrorController();
                errorHandler.ThrowError(pipelineResult.ErrorId);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        if(_input != null)
            _input.UI.Click.performed -= OnClickPerformed;
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
        _loading?.SetProgress(0f, _runningStartupLocale);
        
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
                    CancellationToken effectiveCt = step.HasTimeout ? timeoutCts.Token : CancellationToken.None;
                    var result = await step.RunTasks(_serviceRegistry, effectiveCt);

                    if (!result.IsSuccess)
                    {
                        Debug.LogWarning($"[StartupProcessor] Step failed: {stepName}, error id: {result.ErrorId}, message: {result.Message}");
                        _loading?.SetProgress(1f, _stepFailedLocale);
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
                    _loading?.SetProgress(1f, _stepTimeoutLocale);
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

    private Task WaitForClicked()
    {
        _clickTcs = new TaskCompletionSource<bool>();
        return _clickTcs.Task;
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        _clickTcs?.TrySetResult(true);
    }

    private void OpenMainMenu()
    {
        _input.UI.Disable();
        Debug.Log($"[StartupProcessor] Opening main menu");
        // TODO: Connect to Main Menu Screen Controller
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