using UnityEngine;
using NUnit.Framework;
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
    
    private List<SceneRef> _sceneRefs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnLoadScene;
    }

    private void OnLoadScene(Scene scene, LoadSceneMode mode)
    {
        animator.Play("Scene_Open");
        var canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
    }

    private async Task LoadSceneProcess(string sceneName, bool isInitialize)
    {
        AsyncOperation operation = null;

        if (isInitialize)
        {
            operation = SceneManager.LoadSceneAsync("LoadingScene");

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
        
        animator.Play("Scene_Close");
        
        await AnimationProcess();
        
        if(operation != null)
            operation.allowSceneActivation = true;
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
