using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

[Serializable]
public class SceneRef
{
    public string sceneName;
    public List<string> preloadKeys;
}

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance  { get; private set; }
    [SerializeField] private Animator animator;
    [SerializeField] private Canvas canvas;
    [SerializeField] private List<SceneRef> sceneRefs;
    
    private InitManagerHelper _initManagerHelper;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _initManagerHelper = GetComponent<InitManagerHelper>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnLoadScene; 
        SceneManager.sceneLoaded += OnLoadScene;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLoadScene;
    }

    private void OnLoadScene(Scene scene, LoadSceneMode mode)
    {
        if (animator != null)
        {
            animator.Play("Scene_Open");
        }
        else
        {
            animator = GetComponentInChildren<Animator>();
            animator?.Play("Scene_Open");
        }
        canvas.worldCamera = Camera.main;
    }

    public async Task LoadScene(string sceneName, bool initHelper)
    {
        Debug.Log($"[SceneLoader] Loading scene {sceneName}");
        await LoadSceneProcess(sceneName, initHelper);
    }

    private async Task LoadSceneProcess(string sceneName, bool initHelper)
    {
        AsyncOperation operation = null;

        if (initHelper)
        {
            operation = SceneManager.LoadSceneAsync("Loading");

            _initManagerHelper.preloadGroups = new List<string>();
            
            foreach (var sr in sceneRefs)
            {
                if (sr.sceneName == sceneName)
                {
                    _initManagerHelper.preloadGroups = sr.preloadKeys;
                    break;
                }
            }
            
            _initManagerHelper.loadSceneWhenDone = sceneName;

            if (operation != null)
            {
                while(operation.progress < 0.9f)
                    await Task.Yield();
            }
        }
        else
        {
            operation = SceneManager.LoadSceneAsync(sceneName);
            
            if (operation != null) 
                operation.allowSceneActivation = false;
        }
        
        animator?.Play("Scene_Close");
        
        await AnimationProcess();
        
        if(operation != null)
            operation.allowSceneActivation = true;

        if (initHelper)
        {
            _initManagerHelper.Load();
        }
    }

    private async Task AnimationProcess()
    {
        await Task.Yield();
        var state = animator.GetCurrentAnimatorStateInfo(0);
        
        while (state.normalizedTime < 1f && state.IsName("Scene_Close"))
        {
            await Task.Yield();
            state = animator.GetCurrentAnimatorStateInfo(0);
        }
    }
}
